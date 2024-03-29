﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Demo_WxTool
{
    public partial class Form1 : Form
    {
        private bool IsRunning = false;
        private bool IsStop = false;
        private int successCount = 0;
        public Form1()
        {
            InitializeComponent();
        }

        private void btnDatToImage_Click(object sender, EventArgs e)
        {
            if (IsRunning)
            {
                MessageBox.Show("程序正忙，请耐心等待..."); return;
            }
            try
            {
                FileInfo[] files = null;
                if (File.Exists(txtFolder.Text))
                {
                    files = new FileInfo[] { new FileInfo(txtFolder.Text) };
                }
                else
                {
                    var dir = new DirectoryInfo(txtFolder.Text);
                    if (dir != null)
                    {
                        files = dir.GetFiles();
                    }
                }
                var list = files.Where(p => p.Extension.ToLower().Contains("dat")).ToList();
                if (list.Count == 0)
                    return;

                successCount = 0;
                var xorKey = Convert.ToInt64(txtXor.Text, 16);
                progressBar1.Value = 0;
                progressBar1.Maximum = list.Count;
                btnStop.Enabled = true;
                btnDatToImage.Enabled = false;
                IsRunning = true;
                IsStop = false;
                ThreadPool.SetMinThreads(1, 1);
                ThreadPool.SetMaxThreads(5, 5);

                foreach (var file in list)
                {
                    if (IsStop) break;
                    ThreadPool.QueueUserWorkItem(p => DatToImage(file, xorKey));
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Reset();
            }
            finally
            {
            }
        }
        void DatToImage(FileInfo fileInfo, long key)
        {
            if (!IsStop)
            {
                byte[] data = null;
                using (var fs = new FileStream(fileInfo.FullName, FileMode.Open))
                {
                    data = new byte[fs.Length];
                    int count = fs.Read(data, 0, data.Length);
                    var fileName = fileInfo.FullName + ".jpg";
                    using (var nfs = new FileStream(fileName, FileMode.OpenOrCreate))
                    {
                        foreach (var b in data)
                        {
                            nfs.WriteByte((byte)(b ^ key));
                            nfs.Flush();
                        }
                    }
                    Console.WriteLine("正在处理：" + fileInfo.FullName);
                }

            }
            Interlocked.Increment(ref successCount);
            progressBar1.BeginInvoke((MethodInvoker)delegate { progressBar1.Value++; });
            if (successCount >= progressBar1.Maximum)
                progressBar1.BeginInvoke((MethodInvoker)delegate { Reset(); MessageBox.Show("全部已完成转换"); });

        }

        void DatToImage(FileInfo[] files, string xorKey)
        {
            IsRunning = true;
            var key = Convert.ToInt64(xorKey, 16); ; //本机异或值
            byte[] data = null;
            foreach (var fileInfo in files)
            {
                if (IsStop) break;
                if (fileInfo.Extension.ToLower().Contains("dat"))
                {
                    progressBar1.BeginInvoke((MethodInvoker)delegate { progressBar1.Value++; });
                    using (var fs = new FileStream(fileInfo.FullName, FileMode.Open))
                    {
                        data = new byte[fs.Length];
                        int count = fs.Read(data, 0, data.Length);
                        var fileName = fileInfo.FullName + ".jpg";
                        using (var nfs = new FileStream(fileName, FileMode.OpenOrCreate))
                        {
                            foreach (var b in data)
                            {
                                nfs.WriteByte((byte)(b ^ key));
                                nfs.Flush();
                            }
                        }
                        Console.WriteLine("正在处理：" + fileInfo.FullName);
                    }
                }
                //break;
            }
            progressBar1.BeginInvoke((MethodInvoker)delegate { Reset(); MessageBox.Show("全部已完成转换"); });
        }

        void Reset()
        {

            IsRunning = false;
            IsStop = false;
            btnDatToImage.Enabled = true;
            btnStop.Enabled = false;
            progressBar1.Value = progressBar1.Maximum;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string path = ((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
            txtFolder.Text = path;
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Link;
            }
            else
                e.Effect = DragDropEffects.None;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            IsStop = true;
        }
    }
}
