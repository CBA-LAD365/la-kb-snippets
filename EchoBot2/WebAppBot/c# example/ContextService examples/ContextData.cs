public class ContextData
{
    public AssertedString chatActivityId { get; set; } = null;
    public Customer customer { get; set; } = null;
    public Browser browser { get; set; } = null;
}

public class Customer
{
    public AssertedString id { get; set; } = null;
    public AssertedString firstName { get; set; } = null;
    public AssertedString lastName { get; set; } = null;
    
    public AssertedString os { get; set; } = null;
    public AssertedString country { get; set; } = null;
    public AssertedString city { get; set; } = null;
    public AssertedString isp { get; set; } = null;
    public AssertedString device { get; set; } = null;
}

public class Browser
{
    public AssertedString version { get; set; } = null;
    public AssertedString type { get; set; } = null;
    public PageHistory[] pageHistory { get; set; } = null;
}

public class PageHistory
{
    public AssertedString url { get; set; } = null;
    public AssertedString title { get; set; } = null;
    public AssertedString startTime { get; set; } = null;
}

public class AssertedString
{
    public string value { get; set; } = null;
    public bool isAsserted { get; set; } = false;
}

public class AssertedInteger
{
    public int value { get; set; } = 0;
    public bool isAsserted { get; set; } = false;
}
