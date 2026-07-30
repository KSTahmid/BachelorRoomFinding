using BachelorRoomFinding.Data;
using BachelorRoomFinding.Entities;
using BachelorRoomFinding.Filters;
using BachelorRoomFinding.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BachelorRoomFinding.Controllers
{
    [RequireLogin]
    public class MessController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly NotificationService _notifSvc;

        public MessController(AppDbContext context, IWebHostEnvironment env, NotificationService notifSvc)
        {
            _context = context;
            _env = env;
            _notifSvc = notifSvc;
        }

        private int UserId => HttpContext.Session.GetInt32("UserId")!.Value;

        // ── Digital Board Dashboard ─────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var memberRec = await _context.MessMembers
                .Include(m => m.MessGroup)
                .ThenInclude(g => g!.Room)
                .FirstOrDefaultAsync(m => m.UserId == UserId);

            MessGroup? group = null;
            MessRole myRole = MessRole.Tenant;
            bool isManager = false;

            if (memberRec != null)
            {
                group = memberRec.MessGroup;
                myRole = memberRec.Role;
                isManager = memberRec.IsManager || memberRec.Role == MessRole.Owner || memberRec.Role == MessRole.MessAdmin;
            }
            else
            {
                // Check if user is Owner of a room with an existing MessGroup
                var ownerGroup = await _context.MessGroups
                    .Include(g => g.Room)
                    .FirstOrDefaultAsync(g => g.Room.OwnerId == UserId);

                if (ownerGroup != null)
                {
                    group = ownerGroup;
                    myRole = MessRole.Owner;
                    isManager = true;

                    // Ensure owner record exists in MessMembers
                    var ownerMember = await _context.MessMembers
                        .FirstOrDefaultAsync(m => m.MessGroupId == ownerGroup.Id && m.UserId == UserId);
                    if (ownerMember == null)
                    {
                        _context.MessMembers.Add(new MessMember
                        {
                            MessGroupId = ownerGroup.Id,
                            UserId = UserId,
                            Role = MessRole.Owner,
                            IsManager = true,
                            JoinedAt = DateTime.Now
                        });
                        await _context.SaveChangesAsync();
                    }
                }
            }

            if (group == null)
            {
                return RedirectToAction(nameof(Setup));
            }

            // Load complete MessGroup details
            var fullGroup = await _context.MessGroups
                .Include(g => g.Room).ThenInclude(r => r.Owner)
                .Include(g => g.Manager)
                .Include(g => g.Members).ThenInclude(m => m.User)
                .Include(g => g.Expenses.OrderByDescending(e => e.Date))
                    .ThenInclude(e => e.Shares).ThenInclude(s => s.User)
                .Include(g => g.Notices.OrderByDescending(n => n.CreatedAt)).ThenInclude(n => n.PostedBy)
                .Include(g => g.Rosters.OrderBy(r => r.AssignedDate)).ThenInclude(r => r.AssignedUser)
                .Include(g => g.FundEntries.OrderByDescending(f => f.EntryDate)).ThenInclude(f => f.User)
                .Include(g => g.MenuVotes.OrderByDescending(v => v.ProposedDate)).ThenInclude(v => v.CreatedBy)
                .Include(g => g.DamageReports.OrderByDescending(d => d.ReportedAt)).ThenInclude(d => d.ReportedBy)
                .Include(g => g.MeterReadings.OrderByDescending(m => m.ReadingDate)).ThenInclude(m => m.LoggedBy)
                .Include(g => g.ShoppingListItems.OrderByDescending(s => s.CreatedAt)).ThenInclude(s => s.AddedBy)
                .FirstOrDefaultAsync(g => g.Id == group.Id);

            if (fullGroup == null) return RedirectToAction(nameof(Setup));

            // Summary Calculations for Bangladesh Mess standard
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var monthlyBazarExpenses = fullGroup.Expenses
                .Where(e => e.Category == "Bazar" && e.Date >= startOfMonth)
                .Sum(e => e.Amount);

            var memberCount = fullGroup.Members.Count(m => m.Role != MessRole.Owner);
            decimal perHeadBazar = memberCount > 0 ? monthlyBazarExpenses / memberCount : 0;

            var totalContributions = fullGroup.FundEntries.Where(f => f.EntryType == "Contribution").Sum(f => f.Amount);
            var totalFundExpenses = fullGroup.FundEntries.Where(f => f.EntryType == "CommonExpense").Sum(f => f.Amount)
                + fullGroup.Expenses.Sum(e => e.Amount);
            var netFundBalance = totalContributions - totalFundExpenses;

            ViewBag.IsManager = isManager;
            ViewBag.CurrentUserId = UserId;
            ViewBag.MyRole = myRole;
            ViewBag.MonthlyBazarTotal = monthlyBazarExpenses;
            ViewBag.PerHeadBazar = Math.Round(perHeadBazar, 2);
            ViewBag.TotalContributions = totalContributions;
            ViewBag.TotalFundExpenses = totalFundExpenses;
            ViewBag.NetFundBalance = netFundBalance;

            return View(fullGroup);
        }

        // ── Setup / Join ────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Setup()
        {
            var myApps = await _context.RentalApplications
                .Include(a => a.Room)
                .Where(a => a.ApplicantId == UserId && a.Status == ApplicationStatus.Approved)
                .ToListAsync();

            var myOwnerRooms = await _context.Rooms
                .Where(r => r.OwnerId == UserId)
                .ToListAsync();

            ViewBag.OwnerRooms = myOwnerRooms;
            return View(myApps);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGroup(int roomId, string name)
        {
            var existingGroup = await _context.MessGroups.FirstOrDefaultAsync(g => g.RoomId == roomId);
            if (existingGroup != null)
            {
                TempData["Error"] = "A mess group already exists for this room. You can join it using the invite code.";
                return RedirectToAction(nameof(Setup));
            }

            var room = await _context.Rooms.FindAsync(roomId);
            bool isRoomOwner = room != null && room.OwnerId == UserId;

            var group = new MessGroup
            {
                Name = name,
                RoomId = roomId,
                ManagerUserId = UserId,
                InviteCode = GenerateInviteCode()
            };
            _context.MessGroups.Add(group);
            await _context.SaveChangesAsync();

            var member = new MessMember
            {
                MessGroupId = group.Id,
                UserId = UserId,
                IsManager = true,
                Role = isRoomOwner ? MessRole.Owner : MessRole.MessAdmin
            };
            _context.MessMembers.Add(member);
            await _context.SaveChangesAsync();

            TempData["Success"] = "MessBoard community created successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> JoinGroup(int roomId)
        {
            var group = await _context.MessGroups.FirstOrDefaultAsync(g => g.RoomId == roomId);
            if (group == null)
            {
                TempData["Error"] = "No MessBoard exists for this room yet.";
                return RedirectToAction(nameof(Setup));
            }

            var alreadyMember = await _context.MessMembers.AnyAsync(m => m.MessGroupId == group.Id && m.UserId == UserId);
            if (alreadyMember) return RedirectToAction(nameof(Index));

            _context.MessMembers.Add(new MessMember
            {
                MessGroupId = group.Id,
                UserId = UserId,
                IsManager = false,
                Role = MessRole.Tenant
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "You joined the MessBoard community!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> JoinByInvite(string inviteCode)
        {
            var code = (inviteCode ?? "").Trim().ToUpperInvariant();
            var group = await _context.MessGroups.FirstOrDefaultAsync(g => g.InviteCode == code);
            if (group == null)
            {
                TempData["Error"] = "Invalid invite code. Please check with your Mess Manager or Owner.";
                return RedirectToAction(nameof(Setup));
            }

            var alreadyMember = await _context.MessMembers.AnyAsync(m => m.MessGroupId == group.Id && m.UserId == UserId);
            if (!alreadyMember)
            {
                _context.MessMembers.Add(new MessMember
                {
                    MessGroupId = group.Id,
                    UserId = UserId,
                    Role = MessRole.Tenant,
                    IsManager = false,
                    JoinedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"You joined '{group.Name}' MessBoard community!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RegenerateInviteCode(int groupId)
        {
            var member = await GetCurrentMemberAsync(groupId);
            if (member == null || (!member.IsManager && member.Role != MessRole.Owner))
            {
                TempData["Error"] = "Only Owner or Mess Admin can regenerate invite codes.";
                return RedirectToAction(nameof(Index));
            }

            var group = await _context.MessGroups.FindAsync(groupId);
            if (group != null)
            {
                group.InviteCode = GenerateInviteCode();
                await _context.SaveChangesAsync();
                TempData["Success"] = "New invite code generated!";
            }
            return RedirectToAction(nameof(Index));
        }

        // ── 1. Bazar / Expense Management ─────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExpense(int groupId, string title, decimal amount, string category, List<int> participantIds, IFormFile? receiptImage)
        {
            var member = await GetCurrentMemberAsync(groupId);
            if (member == null)
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction(nameof(Index));
            }

            if (participantIds == null || participantIds.Count == 0)
            {
                // Default to all active members if none selected
                participantIds = await _context.MessMembers
                    .Where(m => m.MessGroupId == groupId)
                    .Select(m => m.UserId)
                    .ToListAsync();
            }

            var expense = new MessExpense
            {
                MessGroupId = groupId,
                AddedByUserId = UserId,
                Title = title,
                Amount = amount,
                Category = string.IsNullOrWhiteSpace(category) ? "Bazar" : category,
                Date = DateTime.Now,
                ReceiptImagePath = await SaveMessReceiptAsync(receiptImage, groupId)
            };

            _context.MessExpenses.Add(expense);
            await _context.SaveChangesAsync();

            decimal splitAmount = amount / Math.Max(participantIds.Count, 1);
            foreach (var pId in participantIds)
            {
                var share = new MessExpenseShare
                {
                    MessExpenseId = expense.Id,
                    UserId = pId,
                    Amount = Math.Round(splitAmount, 2),
                    IsPaid = (pId == UserId)
                };
                if (share.IsPaid) share.PaidAt = DateTime.Now;

                _context.MessExpenseShares.Add(share);
            }
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{category} cost of ৳{amount:N0} split among {participantIds.Count} members.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkSharePaid(int shareId)
        {
            var share = await _context.MessExpenseShares
                .Include(s => s.MessExpense)
                .FirstOrDefaultAsync(s => s.Id == shareId && s.UserId == UserId);
            
            if (share != null && !share.IsPaid)
            {
                share.IsPaid = true;
                share.PaidAt = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Expense share marked as paid!";
            }
            return RedirectToAction(nameof(Index));
        }

        // ── 2. Cleaning & Roster Management ──────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRoster(int groupId, int assignedUserId, string taskType, DateTime assignedDate, string? menuOrNotes)
        {
            var member = await GetCurrentMemberAsync(groupId);
            if (member == null || !(member.Role == MessRole.MessAdmin || member.IsManager || member.Role == MessRole.Owner))
            {
                TempData["Error"] = "Only Mess Admin or Manager can assign rosters.";
                return RedirectToAction(nameof(Index));
            }

            var targetMember = await _context.MessMembers.AnyAsync(m => m.MessGroupId == groupId && m.UserId == assignedUserId);
            if (!targetMember)
            {
                TempData["Error"] = "Assigned user is not in this MessBoard.";
                return RedirectToAction(nameof(Index));
            }

            _context.MessRosterItems.Add(new MessRosterItem
            {
                MessGroupId = groupId,
                AssignedUserId = assignedUserId,
                TaskType = taskType,
                AssignedDate = assignedDate,
                MenuOrNotes = menuOrNotes,
                IsCompleted = false
            });
            await _context.SaveChangesAsync();

            // Notify assigned user
            await _notifSvc.CreateAsync(assignedUserId,
                $"{taskType} Roster Assigned",
                $"You have been assigned for {taskType} on {assignedDate:dd MMM yyyy}.",
                NotificationType.General);

            TempData["Success"] = $"{taskType} roster entry created.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRosterComplete(int rosterId)
        {
            var item = await _context.MessRosterItems.FirstOrDefaultAsync(r => r.Id == rosterId);
            if (item == null) return NotFound();

            var member = await GetCurrentMemberAsync(item.MessGroupId);
            if (member == null || (item.AssignedUserId != UserId && !member.IsManager && member.Role != MessRole.MessAdmin))
            {
                TempData["Error"] = "You cannot complete this roster task.";
                return RedirectToAction(nameof(Index));
            }

            item.IsCompleted = true;
            item.CompletedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Roster task marked as complete!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> SendRosterReminder(int rosterId)
        {
            var item = await _context.MessRosterItems
                .Include(r => r.AssignedUser)
                .FirstOrDefaultAsync(r => r.Id == rosterId);
            if (item == null) return NotFound();

            await _notifSvc.CreateAsync(item.AssignedUserId,
                $"Reminder: {item.TaskType} Task Today",
                $"Friendly reminder to complete your assigned {item.TaskType} task today ({item.AssignedDate:dd MMM yyyy}).",
                NotificationType.General);

            TempData["Success"] = $"Reminder sent to {item.AssignedUser?.UserName}.";
            return RedirectToAction(nameof(Index));
        }

        // ── 3. Mess Fund & Cash Management ────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFundEntry(int groupId, int userId, string entryType, decimal amount, string description)
        {
            var member = await GetCurrentMemberAsync(groupId);
            if (member == null || !(member.Role == MessRole.MessAdmin || member.IsManager))
            {
                TempData["Error"] = "Only Mess Admin can record fund transactions.";
                return RedirectToAction(nameof(Index));
            }

            _context.MessFundEntries.Add(new MessFundEntry
            {
                MessGroupId = groupId,
                UserId = userId,
                EntryType = entryType,
                Amount = amount,
                Description = description,
                EntryDate = DateTime.Now
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Mess Fund {entryType} of ৳{amount:N0} recorded.";
            return RedirectToAction(nameof(Index));
        }

        // ── 4. Cooking Roster & Menu Voting ───────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMenuOption(int groupId, string optionName, string mealType, DateTime proposedDate)
        {
            var member = await GetCurrentMemberAsync(groupId);
            if (member == null) return RedirectToAction(nameof(Index));

            _context.MessMenuVotes.Add(new MessMenuVote
            {
                MessGroupId = groupId,
                CreatedByUserId = UserId,
                OptionName = optionName.Trim(),
                MealType = mealType,
                ProposedDate = proposedDate,
                VoteCount = 1,
                VotedUserIdsCsv = UserId.ToString(),
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Menu option added for voting!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> VoteMenuOption(int voteId)
        {
            var vote = await _context.MessMenuVotes.FindAsync(voteId);
            if (vote == null) return NotFound();

            var votedUsers = (vote.VotedUserIdsCsv ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            if (votedUsers.Contains(UserId.ToString()))
            {
                TempData["Error"] = "You have already voted for this menu option.";
                return RedirectToAction(nameof(Index));
            }

            votedUsers.Add(UserId.ToString());
            vote.VotedUserIdsCsv = string.Join(",", votedUsers);
            vote.VoteCount = votedUsers.Count;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Vote cast successfully!";
            return RedirectToAction(nameof(Index));
        }

        // ── 5. Notice Board ───────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddNotice(int groupId, string title, string body)
        {
            var member = await GetCurrentMemberAsync(groupId);
            if (member == null || !(member.Role == MessRole.Owner || member.Role == MessRole.MessAdmin || member.IsManager))
            {
                TempData["Error"] = "Only Owner or Mess Admin can post notices.";
                return RedirectToAction(nameof(Index));
            }

            _context.MessNotices.Add(new MessNotice
            {
                MessGroupId = groupId,
                PostedByUserId = UserId,
                Title = title,
                Body = body,
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Announcement posted to Notice Board!";
            return RedirectToAction(nameof(Index));
        }

        // ── 6. Utility & Misc (Meter Reading, Damage Report, Shopping List) ──
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDamageReport(int groupId, string title, string description)
        {
            var member = await GetCurrentMemberAsync(groupId);
            if (member == null) return RedirectToAction(nameof(Index));

            var group = await _context.MessGroups.Include(g => g.Room).FirstOrDefaultAsync(g => g.Id == groupId);
            if (group == null) return NotFound();

            var damage = new MessDamageReport
            {
                MessGroupId = groupId,
                ReportedByUserId = UserId,
                Title = title,
                Description = description,
                Status = "Open",
                ReportedAt = DateTime.Now
            };
            _context.MessDamageReports.Add(damage);
            await _context.SaveChangesAsync();

            // Auto-notify Room Owner
            if (group.Room != null && group.Room.OwnerId != UserId)
            {
                await _notifSvc.CreateAsync(group.Room.OwnerId,
                    "New Property Damage Report",
                    $"Tenant in room \"{group.Room.Title}\" reported damage: \"{title}\". Please review in MessBoard.",
                    NotificationType.General);
            }

            TempData["Success"] = "Damage report logged and owner notified.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDamageReportStatus(int reportId, string status)
        {
            var report = await _context.MessDamageReports
                .Include(d => d.MessGroup).ThenInclude(g => g!.Room)
                .FirstOrDefaultAsync(d => d.Id == reportId);
            if (report == null) return NotFound();

            var member = await GetCurrentMemberAsync(report.MessGroupId);
            if (member == null || !(member.Role == MessRole.Owner || member.IsManager))
            {
                TempData["Error"] = "Only Owner or Manager can update damage status.";
                return RedirectToAction(nameof(Index));
            }

            report.Status = status;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Damage report status updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMeterReading(int groupId, string utilityType, decimal currentReading, decimal previousReading, decimal billAmount, string? notes)
        {
            var member = await GetCurrentMemberAsync(groupId);
            if (member == null || !(member.Role == MessRole.MessAdmin || member.Role == MessRole.Owner || member.IsManager))
            {
                TempData["Error"] = "Only Mess Admin or Owner can log meter readings.";
                return RedirectToAction(nameof(Index));
            }

            _context.MessMeterReadings.Add(new MessMeterReading
            {
                MessGroupId = groupId,
                LoggedByUserId = UserId,
                UtilityType = utilityType,
                CurrentReading = currentReading,
                PreviousReading = previousReading,
                BillAmount = billAmount,
                ReadingDate = DateTime.Now,
                Notes = notes
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{utilityType} meter reading logged.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddShoppingItem(int groupId, string itemName, string quantity)
        {
            var member = await GetCurrentMemberAsync(groupId);
            if (member == null) return RedirectToAction(nameof(Index));

            _context.MessShoppingListItems.Add(new MessShoppingListItem
            {
                MessGroupId = groupId,
                AddedByUserId = UserId,
                ItemName = itemName.Trim(),
                Quantity = string.IsNullOrWhiteSpace(quantity) ? "1" : quantity.Trim(),
                IsPurchased = false,
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Item added to shared shopping list.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleShoppingItemPurchased(int itemId)
        {
            var item = await _context.MessShoppingListItems.FindAsync(itemId);
            if (item == null) return NotFound();

            item.IsPurchased = !item.IsPurchased;
            item.PurchasedByUserId = item.IsPurchased ? UserId : null;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ── Helper Utilities ──────────────────────────────────────────
        private async Task<MessMember?> GetCurrentMemberAsync(int groupId)
        {
            return await _context.MessMembers.FirstOrDefaultAsync(m => m.MessGroupId == groupId && m.UserId == UserId);
        }

        private static string GenerateInviteCode()
        {
            return Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        }

        private async Task<string?> SaveMessReceiptAsync(IFormFile? file, int groupId)
        {
            if (file == null || file.Length == 0) return null;
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowed.Contains(ext) || file.Length > 2 * 1024 * 1024)
            {
                TempData["Error"] = "Receipt image must be a JPG, PNG, JPEG, or WEBP image under 2 MB.";
                return null;
            }

            var folder = Path.Combine(_env.WebRootPath, "uploads", "mess", groupId.ToString());
            Directory.CreateDirectory(folder);
            var fileName = $"{Guid.NewGuid()}{ext}";
            var path = Path.Combine(folder, fileName);
            await using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/uploads/mess/{groupId}/{fileName}";
        }
    }
}
