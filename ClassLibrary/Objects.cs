using System;
using System.Collections.Generic;

namespace ClassLibrary.Objects;

public class Object
{
    public int CoordX { get; set; }
    public int CoordY { get; set; }
    public required string ObjectName { get; set; }
    public required string ObjectType { get; set; }
}

public class MoveItem
{
    public required Object Object { get; set; }
}
public class MoveResponse
{
    public required List<MoveItem> Move { get; set; }
}

public class LookItem
{
    public required Object Object { get; set; }
}

public class LookResponse
{
    public required List<LookItem> Look { get; set; }
}
