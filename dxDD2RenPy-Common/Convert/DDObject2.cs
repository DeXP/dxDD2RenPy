
public class Rootobject
{
	public Class1[] Property1 { get; set; }
}

public class Class1
{
	public string[] characters { get; set; }
	public Connection[] connections { get; set; }
	public string editor_version { get; set; }
	public string file_name { get; set; }
	public string[] languages { get; set; }
	public Node[] nodes { get; set; }
	public string selected_language { get; set; }
	public Variables variables { get; set; }
}

public class Variables
{
	public Povname povname { get; set; }
	public Result result { get; set; }
	public Time time { get; set; }
	public Timer_Jump timer_jump { get; set; }
	public Timer_Range timer_range { get; set; }
}

public class Povname
{
	public int type { get; set; }
	public string value { get; set; }
}

public class Result
{
	public int type { get; set; }
	public int value { get; set; }
}

public class Time
{
	public int type { get; set; }
	public int value { get; set; }
}

public class Timer_Jump
{
	public int type { get; set; }
	public string value { get; set; }
}

public class Timer_Range
{
	public int type { get; set; }
	public int value { get; set; }
}

public class Connection
{
	public string from { get; set; }
	public int from_port { get; set; }
	public string to { get; set; }
	public int to_port { get; set; }
}

public class Node
{
	public object[] character { get; set; }
	public Choice[] choices { get; set; }
	public string expand { get; set; }
	public int[] expand_size { get; set; }
	public string file { get; set; }
	public string filename { get; set; }
	public bool is_box { get; set; }
	public string node_name { get; set; }
	public string node_type { get; set; }
	public string object_path { get; set; }
	public float[] offset { get; set; }
	public bool slide_camera { get; set; }
	public int speaker_type { get; set; }
	public object text { get; set; }
	public string title { get; set; }
	public string next { get; set; }
	public int time { get; set; }
	public float rect_size_x { get; set; }
	public float rect_size_y { get; set; }
}

public class Choice
{
	public string condition { get; set; }
	public bool is_condition { get; set; }
	public string next { get; set; }
	public Text text { get; set; }
}

public class Text
{
	public string ENG { get; set; }
}
