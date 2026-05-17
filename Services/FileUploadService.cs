namespace BachelorRoomFinding.Services
{
    public class FileUploadService
    {
        private readonly IWebHostEnvironment _env;
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".pdf" };
        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

        public FileUploadService(IWebHostEnvironment env) => _env = env;

        public async Task<string?> UploadAsync(IFormFile? file, string category, int userId)
        {
            if (file == null || file.Length == 0) return null;

            if (file.Length > MaxFileSizeBytes)
                throw new InvalidOperationException("File size exceeds the 5 MB limit.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                throw new InvalidOperationException("Invalid file type. Allowed: jpg, jpeg, png, pdf.");

            var folder = Path.Combine(_env.WebRootPath, "uploads", category, userId.ToString());
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(folder, fileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/uploads/{category}/{userId}/{fileName}";
        }

        public async Task<List<string>> UploadMultipleAsync(
            IEnumerable<IFormFile>? files, string category, int userId)
        {
            var paths = new List<string>();
            if (files == null) return paths;

            foreach (var file in files)
            {
                var path = await UploadAsync(file, category, userId);
                if (path != null) paths.Add(path);
            }
            return paths;
        }

        public void Delete(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;
            var fullPath = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }
    }
}
