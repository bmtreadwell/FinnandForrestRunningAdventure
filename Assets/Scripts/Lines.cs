using UnityEngine;

// Official NYC subway line colors and real terminals for the services shown on trains and entrance signs.
public static class Lines
{
    public const string All = "1237ACEBDFMNQRWJZL456";

    public static Color Of(char c)
    {
        switch (c)
        {
            case '1': case '2': case '3': return new Color(0.933f, 0.208f, 0.180f);
            case '4': case '5': case '6': return new Color(0.000f, 0.576f, 0.235f);
            case '7': return new Color(0.725f, 0.200f, 0.678f);
            case 'A': case 'C': case 'E': return new Color(0.000f, 0.224f, 0.651f);
            case 'B': case 'D': case 'F': case 'M': return new Color(1.000f, 0.388f, 0.075f);
            case 'G': return new Color(0.424f, 0.745f, 0.271f);
            case 'J': case 'Z': return new Color(0.600f, 0.400f, 0.200f);
            case 'L': return new Color(0.655f, 0.663f, 0.675f);
            default: return new Color(0.988f, 0.800f, 0.039f); // N Q R W
        }
    }

    // Yellow bullets carry black letters.
    public static Color Text(char c)
    {
        return (c == 'N' || c == 'Q' || c == 'R' || c == 'W') ? Color.black : Color.white;
    }

    public static string Terminal(char c)
    {
        switch (c)
        {
            case '1': return "Van Cortlandt Park-242 St";
            case '2': return "Wakefield-241 St";
            case '3': return "Harlem-148 St";
            case '4': return "Woodlawn";
            case '5': return "Eastchester-Dyre Av";
            case '6': return "Pelham Bay Park";
            case '7': return "Flushing-Main St";
            case 'A': return "Inwood-207 St";
            case 'C': return "168 St";
            case 'E': return "Jamaica Ctr-Parsons/Archer";
            case 'B': return "Bedford Park Blvd";
            case 'D': return "Norwood-205 St";
            case 'F': return "Jamaica-179 St";
            case 'M': return "Middle Village-Metropolitan Av";
            case 'N': case 'W': return "Astoria-Ditmars Blvd";
            case 'Q': return "96 St";
            case 'R': return "Forest Hills-71 Av";
            case 'J': case 'Z': return "Jamaica Ctr-Parsons/Archer";
            default: return "8 Av";
        }
    }
}
