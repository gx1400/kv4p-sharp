namespace kv4p_net8_lib.Types;

public class CtcssTones
{
    public Dictionary<int, string> TonesDictionary { get; }

    public CtcssTones()
    {
        TonesDictionary = new Dictionary<int, string>();

        // Add "None" option at index 0
        TonesDictionary.Add(0, "None");

        // Add each CTCSS tone with sequential indices starting from 1
        var tones = new List<double>
        {
            67.0,
            71.9,
            74.4,
            77.0,
            79.7,
            82.5,
            85.4,
            88.5,
            91.5,
            94.8,
            97.4,
            100.0,
            103.5,
            107.2,
            110.9,
            114.8,
            118.8,
            123.0,
            127.3,
            131.8,
            136.5,
            141.3,
            146.2,
            151.4,
            156.7,
            162.2,
            167.9,
            173.8,
            179.9,
            186.2,
            192.8,
            203.5,
            210.7,
            218.1,
            225.7,
            233.6,
            241.8,
            250.3,
        };

        int index = 1;
        foreach (var tone in tones)
        {
            TonesDictionary.Add(index++, $"{tone} Hz");
        }
    }
}
