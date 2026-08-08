using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace Zinc.Core.Abstractions;

public class CompileOptions
{
    public string CodePath { get; set; } = string.Empty;

    public bool enableO2 { get; set; } = true;

    public bool enableGDB { get; set; } = true;

    public CppStandard StandardVersion { get; set; } = CppStandard.Cpp11;

    public bool warningCheck { get; set; } = true;

    public bool overAddressCheck { get; set; } = false;
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