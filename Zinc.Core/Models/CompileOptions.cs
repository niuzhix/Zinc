using System.ComponentModel.DataAnnotations;

namespace Zinc.Core.Models;

/// <summary>一次 C++ 编译请求的参数。</summary>
public class CompileOptions
{
    /// <summary>待编译的源文件路径。</summary>
    public string CodePath { get; set; } = string.Empty;

    /// <summary>指定编译器路径；为空时由 <c>IProgramService</c> 自行发现。</summary>
    public string? CompilerPath { get; set; }

    public bool EnableO2 { get; set; } = true;

    public bool EnableGDB { get; set; } = true;

    public CppStandard StandardVersion { get; set; } = CppStandard.Cpp11;

    public bool WarningCheck { get; set; } = true;

    public bool OverAddressCheck { get; set; }
}

public enum CppStandard
{
    [Display(Name = "c++98")]
    Cpp98 = 1998,

    [Display(Name = "c++03")]
    Cpp03 = 2003,

    [Display(Name = "c++11")]
    Cpp11 = 2011,

    [Display(Name = "c++14")]
    Cpp14 = 2014,

    [Display(Name = "c++17")]
    Cpp17 = 2017,

    [Display(Name = "c++20")]
    Cpp20 = 2020,

    [Display(Name = "c++23")]
    Cpp23 = 2023,

    [Display(Name = "c++26")]
    Cpp26 = 2026
}
