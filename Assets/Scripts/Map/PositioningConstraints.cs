public class PositioningConstraints
{
    public enum Type {
        Any, Top, Bottom, Left, Right, TopLeft, TopRight, BottomLeft, BottomRight, Default
    }

    public int MinVertical { get; protected set; } = 0;
    public int MaxVertical { get; protected set; } = int.MaxValue;
    // TODO prio 7 at some point we could convert inPart/ofPart in minHorizontal/maxHorizontal
    // but it means a lot of refactor in many map positioning methods
    // that would be the right moment to update those using in/ofPart to positioningConstraint too
    public int OfPartsHorizontal { get; protected set; } = 1;
    public int InPartHorizontal { get; protected set; } = 1;

    public static PositioningConstraints None => new PositioningConstraints();

    protected PositioningConstraints(
        int minVertical = 0,
        int maxVertical = int.MaxValue,
        int inPartHorizontal = 1,
        int ofPartsHorizontal = 1
    )
    {
        MinVertical = minVertical;
        MaxVertical = maxVertical;
        InPartHorizontal = ((inPartHorizontal - 1) % ofPartsHorizontal) + 1;
        OfPartsHorizontal = ofPartsHorizontal;
    }

    // TODO prio 3 implement and use
    public void LowerVerticalConstraints() { }
    public void LowerHorizontalConstraints() { }

    public void RemoveVerticalConstraints()
    {
        InPartHorizontal = 1;
        OfPartsHorizontal = 1;
    }

    public void RemoveHorizontalConstraints()
    {
        MinVertical = 0;
        MaxVertical = int.MaxValue;
    }

    public static PositioningConstraints InOfConstraint(int inPartHorizontal = 1, int ofPartsHorizontal = 1)
    {
        return new PositioningConstraints(inPartHorizontal: inPartHorizontal, ofPartsHorizontal: ofPartsHorizontal);
    }

    public static PositioningConstraints CalculateConstraint(Type type, int width, int height)
    {
        int minVertical = 0;
        int maxVertical = int.MaxValue;
        int inPartHorizontal = 1;
        int ofPartsHorizontal = 1;
        switch (type) {
            case Type.Default: // 2/3 top
                minVertical = height / 3;
                break;
            case Type.Any: break;
            case Type.Top: // Half top
                minVertical = height / 2;
                break;
            case Type.Bottom: // Half bottom
                maxVertical = height / 2;
                break;
            case Type.Left: // Half left
                inPartHorizontal = 1;
                ofPartsHorizontal = 2;
                break;
            case Type.Right: // Half right
                inPartHorizontal = 2;
                ofPartsHorizontal = 2;
                break;
            case Type.TopLeft:
                minVertical = height / 2;
                inPartHorizontal = 1;
                ofPartsHorizontal = 2;
                break;
            case Type.TopRight:
                minVertical = height / 2;
                inPartHorizontal = 2;
                ofPartsHorizontal = 2;
                break;
            case Type.BottomLeft:
                maxVertical = height / 2;
                inPartHorizontal = 1;
                ofPartsHorizontal = 2;
                break;
            case Type.BottomRight:
                maxVertical = height / 2;
                inPartHorizontal = 2;
                ofPartsHorizontal = 2;
                break;
        }
        return new PositioningConstraints(minVertical, maxVertical, inPartHorizontal, ofPartsHorizontal);
    }

    public static Type TextToType(string text)
    {
        switch (text) {
            case "Anywhere": return Type.Any;
            case "Top": return Type.Top;
            case "Bottom": return Type.Bottom;
            case "Left": return Type.Left;
            case "Right": return Type.Right;
            case "Top Left": return Type.TopLeft;
            case "Top Right": return Type.TopRight;
            case "Bottom Left": return Type.BottomLeft;
            case "Bottom Right": return Type.BottomRight;
            default: return Type.Default;
        }
    }

    public static string TypeToText(Type type)
    {
        switch (type) {
            case Type.Any: return "Anywhere";
            case Type.Top: return "Top";
            case Type.Bottom: return "Bottom";
            case Type.Left: return "Left";
            case Type.Right: return "Right";
            case Type.TopLeft: return "Top Left";
            case Type.TopRight: return "Top Right";
            case Type.BottomLeft: return "Bottom Left";
            case Type.BottomRight: return "Bottom Right";
            default: return "Default";
        }
    }
}
