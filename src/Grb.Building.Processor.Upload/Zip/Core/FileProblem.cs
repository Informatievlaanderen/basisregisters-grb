namespace Grb.Building.Processor.Upload.Zip.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public abstract class FileProblem : IEquatable<FileProblem>, IEqualityComparer<FileProblem>
    {
        protected FileProblem(string code, string message, IReadOnlyCollection<ProblemParameter> parameters)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        }

        public string Code { get; }
        public IReadOnlyCollection<ProblemParameter> Parameters { get; }
        public string Message { get; }

        public bool Equals(FileProblem? x, FileProblem? y)
        {
            if (ReferenceEquals(x, y)) return true;

            if (x is null) return false;

            if (y is null) return false;

            if (x.GetType() != y.GetType()) return false;

            return x.Code == y.Code
                   && x.Message == y.Message
                   && Equals(x.Parameters, y.Parameters);
        }

        public virtual bool Equals(FileProblem? other)
        {
            return other != null
                   && GetType() == other.GetType()
                   && string.Equals(Code, other.Code, StringComparison.InvariantCultureIgnoreCase)
                   && string.Equals(Message, other.Message)
                   && Parameters.SequenceEqual(other.Parameters);
        }

        public override bool Equals(object? obj)
        {
            return obj is FileProblem other && Equals(other);
        }

        public int GetHashCode(FileProblem obj)
        {
            return HashCode.Combine(obj.Code, obj.Message, obj.Parameters);
        }

        public override int GetHashCode()
        {
            return Parameters.Aggregate(
                Code.GetHashCode() ^ Message.GetHashCode(),
                (current, parameter) => current ^ parameter.GetHashCode());
        }
    }
}
