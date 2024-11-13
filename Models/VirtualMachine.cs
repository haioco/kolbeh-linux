using System;
using Newtonsoft.Json;

public class VirtualMachine
{
    public string Title { get; set; }
    public string StatusTitle { get; set; }
    public int Cpu { get; set; }
    public int Ram { get; set; }
    public int Storage { get; set; }
    public ImageInfo Image { get; set; }
    public PlanInfo Plan { get; set; }
    public CountryInfo Country { get; set; }
}

public class ImageInfo
{
    public string Title { get; set; }
}

public class PlanInfo
{
    public string Title { get; set; }
}

public class CountryInfo
{
    public string Name { get; set; }
} 