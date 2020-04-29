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

		/// <summary>
		/// Owner object (document) of a current node. Required mainly for nodes lookup
		/// </summary>
		private DDObject m_Owner;

		/// <summary>
		/// Helper property to know how many nodes reference the current one
		/// </summary>
		public int InputsCount { get; private set; }

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

		/// <summary>
		/// Helper property to get next Node, but not it's ID
		/// </summary>
		public DDNode NextNode
		{
			get
			{
				return m_Owner.GetNode(next);
			}
		}

		/// <summary>
		/// Escapes quotes from the string
		/// </summary>
		/// <param name="text">Input string</param>
		/// <returns>Escaped string</returns>
		public string EscapeString(string text)
		{
			return text.Replace("\"", @"\""");
		}


		/// <summary>
		/// Gets Raw text and escapes the quotes inside to directly output the text.
		/// </summary>
		/// <param name="textObject">Object to get text from</param>
		/// <returns>Text string</returns>
		public string GetText(Object textObject)
		{
			var text = GetRawText(textObject);

			return EscapeString(text);
		}

		/// <summary>
		/// Gets one string of text for the object.
		/// It tries to respect selected language of the document.
		/// Returns first string if the language not persists.
		/// </summary>
		/// <param name="textObject">Object to get text from</param>
		/// <returns>Text string</returns>
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

		/// <summary>
		/// Initialize node. Main outcome is count of inputs to the node. 
		/// It is summary count of nodes, choices and branches who have curret node as a next one.
		/// </summary>
		/// <param name="owner"></param>
		public void Init(DDObject owner)
		{
			m_Owner = owner;

			InputsCount = owner.nodes.Count(n => node_name.Equals(n.next))
				+ owner.nodes.Count(n => n.choices?.Any(c => node_name.Equals(c.next)) ?? false)
				+ owner.nodes.Count(n => (n.branches as Newtonsoft.Json.Linq.JObject)?.Children()
					.Any(c => node_name.Equals(c.First().ToString())) ?? false);
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

		/// <summary>
		/// nodeId-DDNode lookup for fast getting nodes by ID
		/// </summary>
		private Dictionary<string, DDNode> m_NodesLookup;

		/// <summary>
		/// Helper property to get starting node faster
		/// </summary>
		public DDNode StartNode
		{
			get
			{
				return nodes.Where(n => "start".Equals(n.node_type)).SingleOrDefault();
			}
		}

		/// <summary>
		/// Initialize the object. Required after load
		/// </summary>
		public void Init()
		{
			foreach (var node in this.nodes)
			{
				node.Init(this);
			}

			m_NodesLookup = this.nodes.ToDictionary(n => n.node_name);
		}

		/// <summary>
		/// Helper function to convert string nodeId to DDNode object
		/// </summary>
		/// <param name="nodeId">ID of the node you want to find</param>
		/// <returns>null if there is no such object</returns>
		public DDNode GetNode(string nodeId)
		{
			if (string.IsNullOrEmpty(nodeId))
			{
				return null;
			}

			if (m_NodesLookup.ContainsKey(nodeId))
			{
				return m_NodesLookup[nodeId];
			}

			return null;
		}

		/// <summary>
		/// Loads the Dialogue Designer object from disk
		/// </summary>
		/// <param name="path">Full path to the file</param>
		/// <returns>null if file is wrong</returns>
		public static DDObject Load(string path)
		{
			using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				using (var file = new StreamReader(fileStream, System.Text.Encoding.Default))
				{
					JsonSerializer serializer = new JsonSerializer();

					var ddList = (List<DDObject>)serializer.Deserialize(file, typeof(List<DDObject>));

					return ddList.FirstOrDefault();
				}
			}
		}

		/// <summary>
		/// Checks if findNode belongs to loop started with loopNode
		/// </summary>
		/// <param name="findNode">Node you want to check</param>
		/// <param name="loopNode">In which loop findNode should be</param>
		/// <param name="curNode">Recursion node. Equals to loopNode on a first invokation</param>
		/// <param name="firstMet">Parameter required to stop the search on second met (when the loop was processed by reccusrion)</param>
		/// <returns>Boolen, was findNode found in the loopNode</returns>
		public bool IsNodeInLoop(DDNode findNode, DDNode loopNode, DDNode curNode, bool firstMet = true)
		{
			if ((null == curNode) || (null == findNode) || (null == loopNode))
			{
				return false;
			}

			if ((false == firstMet) && (curNode == loopNode))
			{
				return false;
			}

			if (curNode == findNode)
			{
				return true;
			}

			if (IsNodeInLoop(findNode, loopNode, curNode.NextNode, false))
			{
				return true;
			}

			if ((null != curNode.choices) && (curNode.choices.Count() > 0))
			{
				foreach (var choice in curNode.choices)
				{
					if (IsNodeInLoop(findNode, loopNode, GetNode(choice.next), false))
					{
						return true;
					}
				}
			}

			if (curNode.branches is Newtonsoft.Json.Linq.JObject branches)
			{
				foreach(var branch in branches)
				{
					if (IsNodeInLoop(findNode, loopNode, GetNode(branch.Value.ToString()), false))
					{
						return true;
					}
				}
			}

			return false;
		}
	}
}
