using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace dxDD2RenPy.Helpers
{
	public class StatStreamWriter: StreamWriter
	{
		private int m_TotalWritten = 0;

		public StatStreamWriter(string path) : base(path)
		{
		}

		public new int Write(string value)
		{
			int written = value?.Length ?? 0;

			m_TotalWritten += written;
			base.Write(value);

			return written;
		}

		public new int WriteLine(string value)
		{
			int written = Write(value) + Write(Environment.NewLine);
			m_TotalWritten += written;
			return written;
		}

		public int TotalWritten
		{
			get
			{
				return m_TotalWritten;
			}
		}
	}
}
