namespace TraceBack.Controllers;

public static class CommentUploadValidation
{
    public static bool HasExpectedFiles(HttpRequest request, IFormFile? imageFile, bool required)
    {
        var files = request.Form.Files;
        if (imageFile is null) return !required && files.Count == 0;
        return files.Count == 1 &&
            string.Equals(files[0].Name, "imageFile", StringComparison.Ordinal);
    }
}
