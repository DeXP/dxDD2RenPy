using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace dxDD2RenPy.Convert
{
	public class DDConnection
	{
		public string from { get; set; }
		public string from_port { get; set; }
		public string to { get; set; }
		public string to_port { get; set; }
	}

	public enum DDVarType
	{
		String = 0,
		Integer = 1,
		Boolean = 2
	}

	public class DDVariable
	{
		public DDVarType type { get; set; }
		public string value { get; set; }
	}

	public class DDChoice
	{
		public string condition { get; set; }
        public bool is_condition{ get; set; }
		public string next { get; set; }
		public Object text { get; set; }
	}

	public class DDNode
	{
		public string filename { get; set; }
		public bool is_box { get; set; }
		public string next { get; set; }
		public string node_name { get; set; }
		public string node_type { get; set; }
		public string object_path { get; set; }
		public bool slide_camera { get; set; }
		public int speaker_type { get; set; }
		public string title { get; set; }

		public float? time { get; set; }

		public string value { get; set; }
		public string var_name { get; set; }
		public bool? toggle { get; set; }
		public string operation_type { get; set; }

		public string next_done { get; set; }
		public int possibilities { get; set; }
		public Object branches { get; set; }

		public int chance_1 { get; set; }
		public int chance_2 { get; set; }

		public IList<int> expand_size { get; set; }
		public IList<string> character { get; set; }
		public IList<float> offset { get; set; }
		public Object text { get; set; }
		public IList<DDChoice> choices { get; set; }

		private int m_InputsCount;
		private DDObject m_Owner;

		public string StartName
		{
			get
			{
				return m_Owner.file_name.Replace(' ', '_');
			}
		}

		public string LabelName
		{
			get
			{
				return "node" + node_name;
			}
		}

		public string CounterName
		{
			get
			{
				return "count" + node_name;
			}
		}

		public string RandomName
		{
			get
			{
				return "random" + node_name;
			}
		}

		public int InputsCount
		{
			get 
			{
				return m_InputsCount;
			}
		}

		public string EscapeString(string text)
		{
			return text.Replace("\"", @"\""");
		}

		public string GetText(Object textObject)
		{
			var text = GetRawText(textObject);

			return EscapeString(text);
		}

		public string GetRawText(Object textObject)
		{
			if (textObject is Newtonsoft.Json.Linq.JObject jobj)
			{
				if (jobj.HasValues)
				{
					if (jobj.ContainsKey(this.m_Owner.selected_language))
					{
						return jobj[this.m_Owner.selected_language].ToString();
					}
					else
					{
						return jobj[0].ToString();
					}
				}
			}

			return textObject.ToString();
		}

		public void Init(DDObject owner)
		{
			m_Owner = owner;

			m_InputsCount = owner.nodes.Count(n => this.node_name.Equals(n.next))
				+ owner.nodes.Count(n => n.choices?.Any(c => this.node_name.Equals(c.next)) ?? false)
				+ owner.nodes.Count(n => (n.branches as Newtonsoft.Json.Linq.JObject)?.Children()
					.Any(c => this.node_name.Equals(c.First().ToString())) ?? false)
			;
		}
	}

	public class DDObject
	{
		public string editor_version { get; set; }
		public string file_name { get; set; }
		public string selected_language { get; set; }
		public IList<string> languages { get; set; }
		public IList<string> characters { get; set; }
		public IList<DDConnection> connections { get; set; }
		public IList<DDNode> nodes { get; set; }
		public IDictionary<string, DDVariable> variables { get; set; }

		private Dictionary<string, DDNode> m_NodesLookup;

		public DDNode StartNode
		{
			get
			{
				return nodes.Where(n => "start".Equals(n.node_type)).SingleOrDefault();
			}
		}

		public void Init()
		{
			foreach (var node in this.nodes)
			{
				node.Init(this);
			}

			m_NodesLookup = this.nodes.ToDictionary(n => n.node_name);
		}

		public DDNode GetNode(string key)
		{
			if (string.IsNullOrEmpty(key))
			{
				return null;
			}

			if (m_NodesLookup.ContainsKey(key))
			{
				return m_NodesLookup[key];
			}

			return null;
		}

		public static DDObject Load(string path)
		{
			using (StreamReader file = File.OpenText(path))
			{
				JsonSerializer serializer = new JsonSerializer();

				var ddList = (List<DDObject>)serializer.Deserialize(file, typeof(List<DDObject>));

				return ddList.FirstOrDefault();
			}
		}
	}
}
