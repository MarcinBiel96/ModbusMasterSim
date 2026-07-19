using FluentModbus;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModbusMasterSim
{
    public partial class Form1 : Form
    {
        class DeviceTCP {
            public string name;
            public string endpoint;
            public FluentModbus.ModbusTcpClient client;
            public byte[] data=new byte[0];
            public Task reader;
            public Progress<int> progress;
            public CancellationTokenSource cts = new CancellationTokenSource();
            public List<Dataset> datasets = new List<Dataset>();
            public Form1 parent;
            public Label ConnStatLabel = new Label()
            {
                ForeColor = Color.Firebrick,
                AutoSize = true
            };
            
            public DeviceTCP(Form1 parent,string name, string endpoint)
            {
                this.parent = parent;
                this.name = name;
                ConnStatLabel.Text = name;
                this.endpoint = endpoint;
                this.client = new FluentModbus.ModbusTcpClient
                {
                    ReadTimeout = 1000,
                    WriteTimeout = 1000
                };
                progress = new Progress<int>(percent => {parent.RefreshControls(); });
                reader = Task.Run(() => { ReadClient(this, progress); });

            }
            public struct Dataset{
                public string name;
                //public byte id;
                public ushort adr;
                public ushort len;
                public Dataset(string name, ushort adr, ushort len) { 
                    this.name = name;
                    //this.id = id;
                    this.adr = adr;
                    this.len = len;
                }
            }
            void ReadClient(DeviceTCP cl, IProgress<int> progress)
            {
                while (!cl.cts.IsCancellationRequested)
                {
                    if (cl.client.IsConnected)
                    {
                        int index = 0;
                        for (int i = 0; i < cl.datasets.Count; ++i)
                        {
                            Array.Copy(ReadDataset(cl, 1, cl.datasets[i].adr, cl.datasets[i].len), 0, cl.data, index, cl.datasets[i].len * 2);
                            index += cl.datasets[i].len * 2;
                        }
                    }
                    else
                    {
                        try
                        {
                            this.client.Connect(endpoint);
                            ConnStatLabel.ForeColor = Color.ForestGreen;
                        }
                        catch { 
                            //nie udało się połączyć
                        }
                    }
                    progress.Report(1);
                    Thread.Sleep(500);
                }
            }
            byte[] ReadDataset(DeviceTCP cl, byte id, ushort adr, ushort len) {
                    Span<byte> data;
                try
                {
                    data = cl.client.ReadHoldingRegisters(id, adr, len);
                    return data.ToArray();
                }
                catch {
                    ConnStatLabel.ForeColor = Color.Firebrick;
                    cl.client.Disconnect();
                return new byte[len*2];
                }
            }
        }
        
        readonly static List<DeviceTCP> devices=new List<DeviceTCP>();
        readonly static List<Label> labels=new List<Label>();
        readonly static List<System.Windows.Forms.Button> buttons = new List<System.Windows.Forms.Button>();
        readonly static List<Panel> lamps = new List<Panel>();

        public Form1()
        {
            InitializeComponent();
            ReadConfig(this);
            for (int i = 0; i < devices.Count; ++i) {
                devices[i].ConnStatLabel.Location= new Point(10,16*i+10);
                Controls.Add(devices[i].ConnStatLabel);
            }
            for (int i = 0; i < lamps.Count; i++) Controls.Add(lamps[i]);
            for (int i = 0;labels.Count > i;i++) Controls.Add(labels[i]);
            for (int i =0;i<buttons.Count;i++) Controls.Add(buttons[i]);

        }
        static void ReadConfig(Form1 mainwindow) {
            string[] fl = File.ReadAllLines("config.txt");
            for (int i=0;i<fl.Length;++i) {
                string[] fl2 = fl[i].Split(',');
                switch (fl2[0])
                {
                    case "WINDOW":
                        mainwindow.Text=fl2[1];
                        mainwindow.Location = new Point(int.Parse(fl2[2]), int.Parse(fl2[3]));
                        mainwindow.Size = new Size(int.Parse(fl2[4]), int.Parse(fl2[5]));
                        break;
                    case "DEVICE":
                        if (fl[2].StartsWith("COM")) { 
                        
                        }
                        else {
                            devices.Add(new DeviceTCP(mainwindow, fl2[1], fl2[2]));
                        }
                        break;
                    case "DATASET":
                        for (int j = 0; j < devices.Count; ++j) {
                            if (devices[j].name == fl2[2])
                            {
                                devices[j].datasets.Add(new DeviceTCP.Dataset(fl2[1], ushort.Parse(fl2[3]), ushort.Parse(fl2[4])));
                                Array.Resize(ref devices[j].data,devices[j].data.Length+ ushort.Parse(fl2[4])*2);                                
                            }
                        }
                        break;
                    case "LABEL":
                        Label l = new Label
                        {
                            AutoSize = true,
                            AccessibleDescription = fl2[6],                            
                            Name = fl2[1],
                            Location = new Point(int.Parse(fl2[2]), int.Parse(fl2[3])),
                            Font=new Font(fl2[4],float.Parse(fl2[5])) //■
                        };
                        for (int j = 0; j < devices.Count; ++j) {
                            if (fl2.Length == 10 && devices[j].name == fl2[7])
                            {
                                l.AccessibleName = fl2[9];
                                l.Tag = new ushort[] { (ushort)j, ushort.Parse(fl2[8]) };
                            }
                        }
                        labels.Add(l);
                        break;
                    case "BUTTON":
                        System.Windows.Forms.Button b = new System.Windows.Forms.Button
                        {
                            Text = fl2[6],
                            Name = fl2[1],
                            Location = new Point(int.Parse(fl2[2]), int.Parse(fl2[3])),
                            Size = new Size(int.Parse(fl2[4]), int.Parse(fl2[5]))
                        };
                        for (int j = 0; j < devices.Count; ++j) {
                            if (devices[j].name == fl2[7]) b.Tag = new short[] { (short)j, short.Parse(fl2[8]), short.Parse(fl2[9]) };                        
                        }
                        b.Click += Button_Click;
                        buttons.Add(b);
                        break;
                    case "LAMP":
                        Panel p = new Panel { 
                            Name = fl2[1],
                            Location = new Point(int.Parse(fl2[2]), int.Parse(fl2[3])),
                            Size = new Size(int.Parse(fl2[4]), int.Parse(fl2[5])),
                            BackColor = Color.Firebrick
                        };
                        for (int j = 0; j < devices.Count; ++j)
                        {
                            if (devices[j].name == fl2[6]) p.Tag = new ushort[] { (ushort)j, ushort.Parse(fl2[7]), ushort.Parse(fl2[8])};
                        }
                        lamps.Add(p);
                        break;
                }
            }
        }
        public void RefreshControls() {
            
            for (int i = 0; i < labels.Count; ++i) {
                if (labels[i].Tag != null)
                {
                    ushort[] tab = labels[i].Tag as ushort[];
                    //ushort num = (ushort)(devices[tab[0]].data[tab[1] * 2] * 256 + devices[tab[0]].data[tab[1] * 2 + 1]);
                    short num = BitConverter.ToInt16(new byte[] { devices[tab[0]].data[tab[1] * 2+1], devices[tab[0]].data[tab[1] * 2 ] }, 0);
                    double dou = DoMath(labels[i].AccessibleName.Replace("X", num.ToString()));
                    labels[i].Text = labels[i].AccessibleDescription.Replace("%i", dou.ToString());
                    //labels[i].Text = labels[i].AccessibleDescription.Replace("%i", (devices[tab[0]].data[tab[1]*2]*256+ devices[tab[0]].data[tab[1] * 2+1]).ToString());
                }
                else {
                    labels[i].Text=labels[i].AccessibleDescription;
                }
                labels[i].Refresh();
            }
            for (int i = 0; i < lamps.Count; ++i) {
                ushort[] tab = lamps[i].Tag as ushort[];
                short num = BitConverter.ToInt16(new byte[] { devices[tab[0]].data[tab[1] * 2 + 1], devices[tab[0]].data[tab[1] * 2] }, 0);
                if((num & (1<< tab[2]))==0)lamps[i].BackColor = Color.Firebrick;
                else lamps[i].BackColor = Color.ForestGreen;
                lamps[i].Refresh();
            }
        }
        static private void Button_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Button b = sender as System.Windows.Forms.Button;
            if (b.Tag is ushort[] tab)
            {
                if (devices[tab[0]].client.IsConnected)
                {
                    var WriteClient = Task.Run(() => { devices[tab[0]].client.WriteSingleRegister(1, tab[1], (ushort)((tab[2] << 8) | (tab[2] >> 8))); });
                }
                else
                {
                    MessageBox.Show("Urządzenie jest odłączone.");
                }
            }
            else
            {
                MessageBox.Show("Nie zadeklarowao urządzenia przypisanego do tego przycisku.");
            }
        }        
        private double DoMath(string s) {
            return Convert.ToDouble(new DataTable().Compute(s, null));
        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            for(int i  = 0; i < devices.Count; i++) devices[i].cts.Cancel();
        }
        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Escape) Close();             
        }
    }
}
