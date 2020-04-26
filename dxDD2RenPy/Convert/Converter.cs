using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using dxDD2RenPy.Helpers;
using Newtonsoft.Json;

namespace dxDD2RenPy.Convert
{
	public class Converter
	{
		private Manager m_Manager;

		private DDObject m_Object;
		private DDNode m_CurrentRootNode;

		private string m_OutputFolder;
		private string m_OutputFile;

		private StatStreamWriter m_Writer;

		public string Filename
		{
			get
			{
				return m_Object?.file_name ?? string.Empty;
			}
		}

		public int Written
		{
			get
			{
				return m_Writer?.TotalWritten ?? 0;
			}
		}

		public Converter(Manager manager)
		{
			m_Manager = manager;
		}

		public int Load(string filename)
		{
			var dd = DDObject.Load(filename);

			if (null == dd)
			{
				return -1;
			}

			if (null == dd.StartNode)
			{
				return -2;
			}

			dd.Init();

			m_Object = dd;

			m_OutputFolder = Path.GetDirectoryName(filename) + Path.DirectorySeparatorChar;
			m_OutputFile = m_OutputFolder + dd.file_name + ".rpy";

			return 0;
		}

		public int Convert()
		{
			var labelNodes = m_Object.nodes.Where(n => n.InputsCount > 1);
			int written = 0;

			using (m_Writer = new StatStreamWriter(m_OutputFile))
			{
				written += WriteHeader();

				m_CurrentRootNode = m_Object.StartNode;
				written += m_Writer.WriteLine($"label {m_CurrentRootNode.StartName}:");
				written += ProcessNode(m_CurrentRootNode);

				foreach (var node in labelNodes)
				{
					written += m_Writer.WriteLine(string.Empty);
					written += m_Writer.WriteLine($"label {node.LabelName}:");
					written += WriteSetNvlMode(false, 1);

					DDNode nextNode = null;
					m_CurrentRootNode = node;
					written += ProcessSingleNode(node, ref nextNode);
					written += ProcessNode(nextNode);
				}
			}

			return written;
		}	

		private string GetPadding(int level)
		{
			return new string(' ', 4 * level);
		}

		private int WritePassIfNeed(int written, int level)
		{
			if (0 >= written)
			{
				written = m_Writer.WriteLine($"{GetPadding(level)}pass");
			}

			return written;
		}

		private int JumpNode(DDNode node, int level)
		{
			if ((null != node) && (node.InputsCount > 1))
			{
				return m_Writer.WriteLine($"{GetPadding(level)}jump {node.LabelName}");
			}

			return 0;
		}

		private int ProcessNode(DDNode initNode, int level = 1, bool prevIsBox = false)
		{
			if (null == initNode)
			{
				return 0;
			}

			int written = 0;
			DDNode curNode = initNode;
			DDNode nextNode = m_Object.GetNode(initNode.next);

			// Main loop over single input items
			while ((null != curNode) && (curNode.InputsCount < 2))
			{
				written += ProcessSingleNode(curNode, ref nextNode, level, prevIsBox);

				prevIsBox = curNode.is_box;
				curNode = nextNode;
			}

			if ((null != curNode) && (curNode.InputsCount > 1))
			{
				bool loopJump = curNode.node_type.Equals("repeat") && (curNode == m_CurrentRootNode);

				if (false == loopJump)
				{
					written += JumpNode(curNode, level);
				}
			}

			return written;
		}

		private int ProcessSingleNode(DDNode curNode, ref DDNode nextNode, int level = 1, bool prevIsBox = false)
		{
			int written = 0;

			written += WriteMessage(curNode, ref nextNode, level, prevIsBox);
			written += WriteExecute(curNode, ref nextNode, level, prevIsBox);
			written += WriteWait(curNode, ref nextNode, level, prevIsBox);
			written += WriteConditionBranch(curNode, ref nextNode, level, prevIsBox);
			written += WriteRandomBranch(curNode, ref nextNode, level, prevIsBox);
			written += WriteChanceBranch(curNode, ref nextNode, level, prevIsBox);
			written += WriteRepeat(curNode, ref nextNode, level, prevIsBox);
			written += WriteVariable(curNode, ref nextNode, level, prevIsBox);

			return written;
		}

		private int WriteHeader()
		{
			int written = 0;
			written += m_Writer.WriteLine($"#");
			written += m_Writer.WriteLine($"# dxDD2RenPy by DeXPeriX");
			written += m_Writer.WriteLine($"# The file was generated from {Filename}.json");
			written += m_Writer.WriteLine($"# Generation date: {DateTime.Now}");
			written += m_Writer.WriteLine($"# Please do not edit the file manually");
			written += m_Writer.WriteLine($"# All changes will be lost after regeneration");
			written += m_Writer.WriteLine($"# You can find more info about dxDD2RenPy on https://dexp.in/dxDD2RenPy");
			written += m_Writer.WriteLine($"#");
			return written;
		}

		private int WriteSetNvlMode(bool isNvl, int level)
		{
			string nvlMode = isNvl ? "show" : "hide";
			return m_Writer.WriteLine($"{GetPadding(level)}window {nvlMode}");
		}

		private int WriteMessage(DDNode node, ref DDNode nextNode, int level, bool prevIsBox)
		{	
			if ((null == node) || (false == node.node_type.Equals("show_message")))
			{
				return 0;
			}

			int written = 0;
			string padding = GetPadding(level);
			string curText = node.GetText(node.text);

			if (node.is_box != prevIsBox)
			{
				written += WriteSetNvlMode(node.is_box, level);
			}

			if (node.choices == null)
			{
				string character = node.character[0];

				if ("Player".Equals(character))
				{
					written += m_Writer.WriteLine($"{padding}\"{curText}\"");
				}
				else
				{
					written += m_Writer.WriteLine($"{padding}{character} \"{curText}\"");
				}
			} 
			else
			{
				string menuPadding = GetPadding(level + 1);

				written += m_Writer.WriteLine(string.Empty);
				written += m_Writer.WriteLine($"{padding}menu:");

				if (false == string.IsNullOrEmpty(curText))
				{
					written += m_Writer.WriteLine($"{menuPadding}\"{curText}\"");
				}

				foreach (var choice in node.choices)
				{
					written += m_Writer.WriteLine(string.Empty);
					written += m_Writer.Write($"{menuPadding}\"{node.GetText(choice.text)}\"");

					if (choice.is_condition)
					{
						written += m_Writer.Write($" if {choice.condition}");
					}

					written += m_Writer.WriteLine($":");

					written += WritePassIfNeed(ProcessNode(m_Object.GetNode(choice.next), level + 2, node.is_box), level + 2);
				}
			}

			nextNode = m_Object.GetNode(node.next);
			return written;
		}

		private int WriteExecute(DDNode node, ref DDNode nextNode, int level, bool prevIsBox)
		{	
			if ((null == node) || (false == node.node_type.Equals("execute")))
			{
				return 0;
			}

			var cmd = node.text.ToString().Split(' ');

			if ((cmd.Length >= 2) && ("jump".Equals(cmd[0]) || "call".Equals(cmd[0])))
			{
				m_Manager.ProcessFile(m_OutputFolder + cmd[1] + ".json");
			}

			nextNode = m_Object.GetNode(node.next);
			string padding = GetPadding(level);
			return m_Writer.WriteLine($"{padding}{node.text}");
		}

		private int WriteWait(DDNode node, ref DDNode nextNode, int level, bool prevIsBox)
		{
			if ((null == node) || (false == node.node_type.Equals("wait")) || (false == node.time.HasValue))
			{
				return 0;
			}

			nextNode = m_Object.GetNode(node.next);
			return m_Writer.WriteLine($"{GetPadding(level)}$ renpy.pause({node.time.Value.ToString().Replace(',', '.')})");
		}


		private int WriteConditionBranch(DDNode node, ref DDNode nextNode, int level, bool prevIsBox)
		{
			if ((null == node) || (false == node.node_type.Equals("condition_branch")))
			{
				return 0;
			}

			if (node.branches is Newtonsoft.Json.Linq.JObject branches)
			{
				string padding = GetPadding(level);
				int written = 0;

				written += m_Writer.WriteLine($"{padding}if {node.text}:");
				written += WritePassIfNeed(ProcessNode(m_Object.GetNode(branches["True"].ToString()), level + 1, prevIsBox), level + 1);

				if (branches.ContainsKey("False"))
				{
					written += m_Writer.WriteLine($"{padding}else:");
					written += WritePassIfNeed(ProcessNode(m_Object.GetNode(branches["False"].ToString()), level + 1, prevIsBox), level + 1);
				}

				nextNode = m_Object.GetNode(node.next);
				return written;
			}

			return 0;
		}

		private int WriteRandomBranch(DDNode node, ref DDNode nextNode, int level, bool prevIsBox)
		{
			if ((null == node) || (false == node.node_type.Equals("random_branch")))
			{
				return 0;
			}

			if (node.branches is Newtonsoft.Json.Linq.JObject branches) {
				string padding = GetPadding(level);
				bool conditionWritten = false;
				int written = 0;

				written += m_Writer.WriteLine($"{padding}$ {node.RandomName} = renpy.random.randint(1, {node.possibilities})");

				for (int i = 1; i <= node.possibilities; i++)
				{
					string key = i.ToString();

					if (branches.ContainsKey(key))
					{
						DDNode caseNode = m_Object.GetNode(branches[key].ToString());
						string ifName = conditionWritten ? "elif" : "if";
						conditionWritten = true;

						written += m_Writer.WriteLine($"{padding}{ifName} {node.RandomName} == {i}:");
						written += WritePassIfNeed(ProcessNode(caseNode, level + 1, prevIsBox), level + 1);
					}
				}

				nextNode = m_Object.GetNode(node.next);
				return written;
			}

			return 0;
		}

		private int WriteChanceBranch(DDNode node, ref DDNode nextNode, int level, bool prevIsBox)
		{
			if ((null == node) || (false == node.node_type.Equals("chance_branch")))
			{
				return 0;
			}

			if (node.branches is Newtonsoft.Json.Linq.JObject branches)
			{
				string padding = GetPadding(level);
				int written = 0;

				written += m_Writer.WriteLine($"{padding}$ {node.RandomName} = renpy.random.randint(1, 100)");

				written += m_Writer.WriteLine($"{padding}if {node.RandomName} <= {node.chance_1}:");
				written += WritePassIfNeed(ProcessNode(m_Object.GetNode(branches["1"].ToString()), level + 1, prevIsBox), level + 1);

				written += m_Writer.WriteLine($"{padding}else:");
				written += WritePassIfNeed(ProcessNode(m_Object.GetNode(branches["2"].ToString()), level + 1, prevIsBox), level + 1);

				nextNode = m_Object.GetNode(node.next);
				return written;
			}

			return 0;
		}

		private int WriteRepeat(DDNode node, ref DDNode nextNode, int level, bool prevIsBox)
		{
			if ((null == node) || (false == node.node_type.Equals("repeat")))
			{
				return 0;
			}

			int written = 0;
			string pad = GetPadding(level);

			written += m_Writer.WriteLine($"{pad}$ {node.CounterName} = 0");
			written += m_Writer.WriteLine($"{pad}while {node.CounterName} < {node.value}:");

			written += ProcessNode(m_Object.GetNode(node.next), level + 1, prevIsBox);
			written += m_Writer.WriteLine($"{GetPadding(level + 1)}$ {node.CounterName} += 1");

			nextNode = m_Object.GetNode(node.next_done);
			return written;
		}

		private int WriteVariable(DDNode node, ref DDNode nextNode, int level, bool prevIsBox)
		{
			if ((null == node) || (false == node.node_type.Equals("set_local_variable")))
			{
				return 0;
			}

			int written = 0;
			string pad = GetPadding(level);
			string varValue = node.EscapeString(node.value);
			DDVarType varType = DDVarType.String;

			if (m_Object.variables.ContainsKey(node.var_name)) 
			{
				varType = m_Object.variables[node.var_name].type;
			}

			written += m_Writer.Write($"{pad}$ {node.var_name} ");

			switch (node.operation_type)
			{
				case "SUBSTRACT": written += m_Writer.Write($"-="); break;
				case "ADD": written += m_Writer.Write($"+="); break;
				default: written += m_Writer.Write($"="); break;
			}

			switch (varType)
			{
				case DDVarType.String:
					written += m_Writer.Write($" \"{varValue}\"");
					break;

				case DDVarType.Integer:
					written += m_Writer.Write($" {varValue}"); 
					break;

				case DDVarType.Boolean:
					if ("true".Equals(node.toggle))
					{
						written += m_Writer.Write($" not {node.var_name}");
					}
					else
					{
						written += m_Writer.Write(string.Format(" {0}", varValue.Equals("true") ? "True" : "False"));
					}
				break;
			}

			written += m_Writer.WriteLine(string.Empty);

			nextNode = m_Object.GetNode(node.next);
			return written;
		}
	}
}
