namespace Grb.Building.Processor.Upload.Zip.Core
{
    public class FileError : FileProblem
    {
        public FileError(string code, string message, params ProblemParameter[] parameters)
            : base(code, message, parameters)
        {
        }
    }
}
