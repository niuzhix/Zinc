using Zinc.Core.Models;

namespace Zinc.Core.Abstractions;

public interface IOutputComparer
{
    ComparisonResult Compare(string actual, string expected);
}
