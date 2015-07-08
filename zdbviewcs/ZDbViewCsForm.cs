﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace zdbviewcs {
	/// <summary>
	/// zyl910's Database viewer (C#) (C#版数据库查看器).
	/// </summary>
	public partial class ZDbViewCsForm : Form {
		/// <summary>
		/// 当前数据库提供者.
		/// </summary>
		private DbProviderFactory m_provider = null;

		/// <summary>
		/// 当前数据库连接.
		/// </summary>
		private DbConnection m_conn = null;

		/// <summary>
		/// 当前表名.
		/// </summary>
		private string m_CurTableName = null;


		/// <summary>
		/// 将数据表转为xml.
		/// </summary>
		/// <param name="xmlDS"></param>
		/// <returns></returns>
		private string ConvertDataTableToXML(DataTable xmlDS) {
			MemoryStream stream = null;
			XmlTextWriter writer = null;
			try {
				stream = new MemoryStream();
				writer = new XmlTextWriter(stream, Encoding.UTF8);
				writer.Formatting = Formatting.Indented;
				xmlDS.WriteXml(writer);
				int count = (int)stream.Length;
				byte[] arr = new byte[count];
				stream.Seek(0, SeekOrigin.Begin);
				stream.Read(arr, 0, count);
				UTF8Encoding utf = new UTF8Encoding();
				return utf.GetString(arr).Trim();
			}
			catch {
				return String.Empty;
			}
			finally {
				if (writer != null) writer.Close();
			}
		}

		/// <summary>
		/// 输出日志.
		/// </summary>
		/// <param name="s">日志文本.</param>
		public void OutLog(string s) {
			if (txtLog.Lines.Length > 3000) txtLog.Clear();
			txtLog.Select(txtLog.Text.Length, 0);
			txtLog.SelectedText = s + Environment.NewLine;
			txtLog.Select(txtLog.Text.Length, 0);
		}

		/// <summary>
		/// 显示数据表信息.
		/// </summary>
		/// <param name="tablename">表名</param>
		/// <returns></returns>
		private bool DoShowTableInfo(string tablename) {
			bool rt = false;
			// 数据.
			rt = true;
			return rt;
		}

		public ZDbViewCsForm() {
			InitializeComponent();
		}

		private void mnuPopupProviderRefresh_Click(object sender, EventArgs e) {
			// 刷新提供者列表.
			DataTable dt = DbProviderFactories.GetFactoryClasses();
			OutLog(ConvertDataTableToXML(dt));
			// cboFactory.
			cboProvider.BeginUpdate();
			cboProvider.Items.Clear();
			try {
				foreach (DataRow dr in dt.Rows) {
					object s = dr["InvariantName"];
					cboProvider.Items.Add(s);
				}
			}
			finally {
				cboProvider.EndUpdate();
			}
		}

		private void ZDbViewCsForm_Load(object sender, EventArgs e) {
			cboClickCmd.SelectedIndex = 1;
			mnuPopupProviderRefresh_Click(mnuPopupProviderRefresh, null);
		}

		private void btnOpen_Click(object sender, EventArgs e) {
			btnClose_Click(sender, null);
			// main.
			string sFactory = cboProvider.Text.Trim();
			try {
				m_provider = DbProviderFactories.GetFactory(sFactory);
			}
			catch (Exception ex) {
				OutLog(ex.Message);
				MessageBox.Show(this, ex.ToString(), this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			if (null == m_provider) return;
			try {
				// DbConnection.
				try {
					OutLog("Create DbConnection:");
					m_conn = m_provider.CreateConnection();
					OutLog(string.Format("\t{0}", m_conn.GetType().ToString()));
				}
				catch (Exception ex) {
					OutLog(ex.Message);
					MessageBox.Show(this, ex.ToString(), this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
					try {
						m_conn.Dispose();
					}
					catch (Exception ex2) {
						OutLog(ex2.ToString());
					}
					m_conn = null;
					return;
				}
				if (null == m_conn) return;
				// open.
				try {
					m_conn.ConnectionString = txtConnstr.Text.Trim();
					m_conn.Open();
				}
				catch (Exception ex) {
					OutLog(ex.Message);
					MessageBox.Show(this, ex.ToString(), this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
					try {
						m_conn.Dispose();
					}
					catch (Exception ex2) {
						OutLog(ex2.ToString());
					}
					m_conn = null;
					return;
				}
			}
			finally {
				if (null == m_conn) {
					m_provider = null;
				}
			}
			// done.
			bool isconn = true;
			btnOpen.Enabled = !isconn;
			btnClose.Enabled = isconn;
			btnExec.Enabled = isconn;
			cboProvider.Enabled = !isconn;
			txtConnstr.ReadOnly = isconn;
			OutLog("Db opened.");
			// show tables.
			DataTable dt = m_conn.GetSchema("Tables");
			grdTable.DataSource = dt;
			m_CurTableName = null;
		}

		private void btnClose_Click(object sender, EventArgs e) {
			m_provider = null;
			if (null == m_conn) return;
			try {
				m_conn.Close();
				m_conn.Dispose();
			}
			catch (Exception ex) {
				OutLog(ex.ToString());
			}
			m_conn = null;
			// done.
			bool isconn = false;
			btnOpen.Enabled = !isconn;
			btnClose.Enabled = isconn;
			btnExec.Enabled = isconn;
			cboProvider.Enabled = !isconn;
			txtConnstr.ReadOnly = isconn;
			OutLog("Db closed.");
			grdTable.DataSource = null;
			m_CurTableName = null;
		}

		private void btnExec_Click(object sender, EventArgs e) {
			//
		}

		private void ZDbViewCsForm_FormClosed(object sender, FormClosedEventArgs e) {
			if (null != m_conn) {
				try {
					m_conn.Close();
				}
				catch (Exception ex) {
					Trace.TraceError(ex.ToString());
				}
				m_conn = null;
			}
			m_provider = null;
		}

		private void grdTable_CurrentCellChanged(object sender, EventArgs e) {
			string stable = null;
			try {
				stable = grdTable.CurrentRow.Cells["TABLE_NAME"].Value.ToString();
			}
			catch (Exception ex) {
				OutLog(ex.Message);
				return;
			}
			if (string.IsNullOrEmpty(stable)) return;
			if (stable.Equals(m_CurTableName)) return;
			m_CurTableName = stable;
			Trace.TraceInformation("Current table: {0}", m_CurTableName);
			DoShowTableInfo(m_CurTableName);
		}

	}
}