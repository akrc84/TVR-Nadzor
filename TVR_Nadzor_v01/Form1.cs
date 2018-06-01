using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using SnmpSharpNet;
using System.Reflection;
using System.Collections;
using System.Text.RegularExpressions;

//MOJ PRVI RELATIVNO RESNI PROGRAM. SE OPRAVIČUJEM (BOŠ ŽE VIDU ZAKAJ). LP Aleš, 12.6.2017

namespace TVR_Nadzor_v01
{
    public partial class Form1 : Form
    {

        //STRING PATH ZA BAZE - baze so v public zato da jih program lahko ureja....fakin windows permissioni
        BasePaths bp = new BasePaths();
        
        //LIST SAT KANALOV
        List<SatKanali> SatKanali = new List<SatKanali>();

        //LISTI S PODATKI ZA SPREJEMNIKE
        List<string> LBMInputLIST = new List<string>();

        //NOVA BOLJŠA BAZA - LIST S SPREJEMNIKI
        List<Sprejemniki1> Sprejemniki1 = new List<Sprejemniki1>();
        Sprejemniki1 IzbranSPR = new Sprejemniki1();

        //List LBM podatkov
        List<LBM> LBM_data = new List<LBM>();

        //List BISS podatkov
        List<BISS> BISS_data = new List<BISS>();

        //List za OID
        List<OID> OID_data = new List<OID>();

        //LISTI S PODATKI ZA ENKODERJE IN PC-JE
        List<Enkoderji> ENC_data = new List<Enkoderji>();
        Enkoderji IzbranENC = new Enkoderji();

        //LISTI S PODATKI ZA MULTICASTE
        List<Multicasts> Multi_data = new List<Multicasts>();
        Multicasts IzbranMC = new Multicasts();

        //VLAN Baza
        List<VLANs> VLANs_data = new List<VLANs>();

        //SWITCH BAZA
        List<SwitchData> Switch_data = new List<SwitchData>();
        SwitchData Izbranswitch = new SwitchData();

        public Form1()
        {
            InitializeComponent();

            //Prepreči utripanje ozadij
            this.DoubleBuffered = true;
            foreach (Control control in this.Controls)
            {
                control.EnableDoubleBuferring();
            }
            
            //Polnjenje lista SAT kanalov
            try
            {
                using (FileStream fs = new FileStream(bp.Kanali_CSP_Baza_path, FileMode.Open, FileAccess.Read))//"using" da pocisti za sabo resource da ga lahko uporabljajo tudi drugi ko gre iz tega "scoupa" po domac do }
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        sr.ReadLine();//spustimo 2 vrstice ker so junk
                        sr.ReadLine();

                        while (!sr.EndOfStream)
                        {
                            string[] SatData = sr.ReadLine().Split(',');
                            SatKanali data = new SatKanali();
                            data.Satelit = SatData[0];
                            if (Satelit_comboBox.Items.Contains(SatData[0]) != true)
                            {
                                Satelit_comboBox.Items.Add(SatData[0]);
                            }

                            data.Modulacija = SatData[7];
                            if (Modulacija_comboBox.Items.Contains(SatData[7]) != true)
                            {
                                Modulacija_comboBox.Items.Add(SatData[7]);
                            }

                            string[] kanal = SatData[1].Split(' ');
                            data.Kanal = kanal[0];
                            data.frekvence = SatData[2];
                            data.SR = SatData[4];
                            data.polarizacija = SatData[3];
                            SatKanali.Add(data);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            //Polnjenje lista Sprejemnikov
            try
            {
                using (FileStream fs = new FileStream(bp.Sprejemniki_CSP_Baza_path, FileMode.Open, FileAccess.Read))//"using" da pocisti za sabo resource da ga lahko uporabljajo tudi drugi ko gre iz tega "scoupa" po domac do }
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        while (!sr.EndOfStream)
                        {
                            string[] SprData = sr.ReadLine().Split(',');
                            Sprejemniki1 data = new Sprejemniki1();
                            data.Ime = SprData[0];
                            data.Proizvajalec = SprData[1];
                            data.IP = SprData[2];
                            data.SwitchPort1 = SprData[3];
                            data.Switchport2 = SprData[4];
                            data.Tip = SprData[5];
                            data.LMBPort = SprData[6];
                            data.SigNoiseOID = SprData[7];
                            data.LockedOID = SprData[8];
                            data.Switch = SprData[9];
                            Sprejemniki1.Add(data);

                            Sprejemniki.Items.Add(data.Ime);
                            SprNastav_cBox.Items.Add(data.Ime);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            //Polnjenje LBM lista
            try
            {
                using (FileStream fs = new FileStream(bp.LBM_CSP_Baza_path, FileMode.Open, FileAccess.Read))//"using" da pocisti za sabo resource da ga lahko uporabljajo tudi drugi ko gre iz tega "scoupa" po domac do }
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        sr.ReadLine();//spustimo 1 vrstico

                        while (!sr.EndOfStream)
                        {
                            string[] LBMData = sr.ReadLine().Split(',');
                            LBM data = new LBM();
                            data.Ime = LBMData[0];
                            data.InputPort = LBMData[1];
                            LBM_data.Add(data);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            
            //napolni sidebar
            foreach (Sprejemniki1 c in Sprejemniki1)
            {
                SidebarStatsLayoutPanel.RowCount = SidebarStatsLayoutPanel.RowCount + 1;
                SidebarStatsLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                SidebarStatsLayoutPanel.Controls.Add(new Label() { Name = c.Ime, Text = c.Ime.ToUpper() + " / ", Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.MiddleCenter }, SidebarStatsLayoutPanel.RowCount + 1, 0);

            }

            //Napolni BISS list
            try
            {
                using (FileStream fs = new FileStream(bp.BISS_CSP_Baza_path, FileMode.Open, FileAccess.Read))//"using" da pocisti za sabo resource da ga lahko uporabljajo tudi drugi ko gre iz tega "scoupa" po domac do }
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        while (!sr.EndOfStream)
                        {
                            string[] BISSData = sr.ReadLine().Split(',');
                            BISS data = new BISS();
                            data.Sprejemnik = BISSData[0];
                            if(BISSData[1] == null)
                            {
                                data.Koda = "0";
                            }
                            else
                            {
                                data.Koda = BISSData[1];
                            }
                            BISS_data.Add(data);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            //Napolni OID list
            try
            {
                using (FileStream fs = new FileStream(bp.OID_CSP_Baza_path, FileMode.Open, FileAccess.Read))//"using" da pocisti za sabo resource da ga lahko uporabljajo tudi drugi ko gre iz tega "scoupa" po domac do }
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        while (!sr.EndOfStream)
                        {
                            string[] OIDData = sr.ReadLine().Split(',');
                            OID data = new OID();
                            data.Ime = OIDData[0];
                            data.SigNoiseOID = OIDData[1];
                            data.LockedOID = OIDData[2];
                            OID_data.Add(data);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            //Napolni enkoder list
            try
            {
                using (FileStream fs = new FileStream(bp.ENC_CSP_Baza_path, FileMode.Open, FileAccess.Read))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        while (!sr.EndOfStream)
                        {
                            string[] ENCData = sr.ReadLine().Split(',');
                            Enkoderji data = new Enkoderji();
                            data.Ime = ENCData[0];
                            data.IP = ENCData[1];
                            data.Switchport = ENCData[2];
                            data.Switch = ENCData[3];
                            ENC_data.Add(data);
                            ENC_Nastav_cBox.Items.Add(data.Ime);
                            IzberiENC_PCcomboBox.Items.Add(data.Ime);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            //Napolni VLAN list
            try
            {
                using (FileStream fs = new FileStream(bp.VLAN_CSP_Baza_path, FileMode.Open, FileAccess.Read))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        while (!sr.EndOfStream)
                        {
                            string[] VLData = sr.ReadLine().Split(',');
                            VLANs data = new VLANs();
                            data.Ime = VLData[0];
                            data.Stevilo = VLData[1];
                            Vlan1comboBox.Items.Add(VLData[1] + " - " + VLData[0]);
                            VLAN_ENC_PCcomboBox.Items.Add(VLData[1] + " - " + VLData[0]);
                            VLANs_data.Add(data);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            //Napolni MULTICAST list
            try
            {
                using (FileStream fs = new FileStream(bp.Multi_CSP_Baza_path, FileMode.Open, FileAccess.Read))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        while (!sr.EndOfStream)
                        {
                            string[] MCData = sr.ReadLine().Split(',');
                            Multicasts data = new Multicasts();
                            data.Ime = MCData[0];
                            data.IP = MCData[1];
                            data.UDP = MCData[2];
                            data.VLAN = MCData[3];
                            Multi_data.Add(data);
                            MultiPresetsComboBox.Items.Add(data.Ime);
                            Multi_Nastav_cBox.Items.Add(data.Ime);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            
            //Napolni Switch list
            try
            {
                using (FileStream fs = new FileStream(bp.Switch_CSP_Baza_path, FileMode.Open, FileAccess.Read))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        while (!sr.EndOfStream)
                        {
                            string[] SwitchData = sr.ReadLine().Split(',');
                            SwitchData data = new SwitchData();
                            data.Ime = SwitchData[0];
                            data.IP = SwitchData[1];
                            Switch_data.Add(data);
                            Nastav_Switchi_cBox.Items.Add(data.Ime);
                            Nastav_SPR_Switch_comboBox.Items.Add(data.Ime);
                            Nastav_ENC_Switch_comboBox.Items.Add(data.Ime);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            //Napolni LBM combobox
            foreach (LBM c in LBM_data)
            {
                LBMcomboBox.Items.Add(c.Ime);
            }
            
            //ZAŽENI REFRESH TIMER
            timer1.Start();
            
            //refresh status sidebara
            SidebarTimer.Start();
        }

        //PARAMETRI KI JIH ŠE NISEM DAL DRUGAM...
        string targetIP;
        string SwitchIP48 = "192.168.216.245";
        string LBM_matrix_IP = "192.168.216.175";
        
        private void IzberiSprejemnik_Click(object sender, EventArgs e)
        {
            errorIP = "";
            errorSwitch = 0;
            
            IzbranSPR = Sprejemniki1.FirstOrDefault(p => p.Ime == Sprejemniki.Text);

            if (Sprejemniki.Text != "")
            {
                ImeSprejemnikaButton.Text = Sprejemniki.Text;
                ImeSprejemnikaButton.Enabled = true;
                TipSprejemnikaLabel.Text = IzbranSPR.Proizvajalec + " " + IzbranSPR.Tip;
                kanal_textbox.Clear();

                //OMOGOČI KONTROLNA POLJA
                IzberiInputcomboBox.Enabled = true;
                IzberiServicecomboBox.Enabled = true;

                IzberiInputcomboBox.SelectedIndex = -1;
                IzberiServicecomboBox.SelectedIndex = -1;
                BISStextBox.Text = "";

                //ČE JE IZBRAN ERICSSON
                if (IzbranSPR.Proizvajalec == "ericsson")
                {
                    IzbranERICSSON();
                }

                //ČE JE IZBRAN ATEME
                else if (IzbranSPR.Proizvajalec == "ateme")
                {
                    IzbranATEME();
                }
                else if (IzbranSPR.Proizvajalec == "drugi" || IzbranSPR.Proizvajalec == "aviwest")
                {
                    izbranDrugi();
                }
            }

            //VLAN NA CISCO SWITCHU
            if (IzbranSPR.Tip == "SAT in IP" || IzbranSPR.Tip == "IP ONLY")
            {
                if(pingTarget(SwitchIP48) == false)
                {
                    IpIN1VLANLabel.Text = "";
                }
                IpIN1VLANLabel.Text = "VLAN: " + ReadSNMP(SwitchIP48, "1.3.6.1.4.1.9.9.68.1.2.2.1.2.101" + IzbranSPR.SwitchPort1);
            }

            else
            {
                IpIN1VLANLabel.Text = "";
            }

            FrekvencaTextBox.Text = "";
            SymRateTextBox.Text = "";
            LNBTextBox.Text = "";
            MultiPresetsComboBox.SelectedIndex = -1;

        }

        //OSTALE NAPREVE - IZBERI
        private void IzberiENC_PCbutton_Click(object sender, EventArgs e)
        {
            IzbranENC = ENC_data.FirstOrDefault(p => p.Ime == IzberiENC_PCcomboBox.Text);
            errorIP = "";
            errorSwitch = 0;
            
            if (IzberiENC_PCcomboBox.Text != "")
            {
                Enkoderji e1 = ENC_data.FirstOrDefault(p => p.Ime == IzberiENC_PCcomboBox.Text);
                VLAN_ENC_PCcomboBox.Enabled = true;
                izbranENCPClabel.Text = e1.Ime;
                ENC_PC_VLANlabel.Text = "VLAN: " + ReadSNMP(IZBRSwitch(e1.Switch), "1.3.6.1.4.1.9.9.68.1.2.2.1.2.101" + e1.Switchport);
            }

            Ostale_Vmesnik_button.Enabled = true;
            Ostale_Vmesnik_button.Text = IzbranENC.Ime;
        }

        //PREVERJANJE DOSTOPNOSTI NAPRAVE
        public bool pingTarget(string IP)
        {
            if(IP == "0.0.0.0")
            {
                return false;
            }

            int k = 0;
            Ping pingSender = new Ping();
            PingReply reply = pingSender.Send(IP, 250);

            for (int i = 1; i < 2; i++)
            {
                if (reply.Status == IPStatus.Success && IP != SwitchIP48 && d.SelectedTab == MainTab_SPR)
                {
                    SPR_PingStatusLebel.Text = "PING STATUS: SPR. DOSEGLJIV!";
                    SPR_PingStatusLebel.BackColor = Color.Green;
                    k++;
                }
                else if (reply.Status == IPStatus.Success)
                {
                    k++;
                }
            }

            if (k > 0 && errorIP != targetIP)
            {
                timer1.Start();
                return true;
            }
            timer1.Stop();
            RePingtimer.Start();
            return false;
        }

        public bool ping(string IP)
        {
            if (IP == "0.0.0.0")
            {
                return false;
            }

            int k = 0;
            Ping pingSender = new Ping();
            PingReply reply = pingSender.Send(IP, 250);

            for (int i = 1; i < 2; i++)
            {
                if (reply.Status == IPStatus.Success)
                {
                    k++;
                }
            }

            if (k > 0 && errorIP != targetIP)
            {
                return true;
            }
            return false;
        }

        //DVOJNO PREVERJANJE DOSTOPNOSTI
        int errorSwitch = 0;
        string errorIP;
        public void REpingTarget(string IP)
        {
            if(ValidateIPv4(IP) == false)
            {
                return;
            }

            int k = 0;
            Ping pingSender = new Ping();

            PingReply reply = pingSender.Send(IP, 250);

            for (int i = 1; i < 3; i++)
            {
                if (reply.Status == IPStatus.Success)
                {
                    k++;
                }
            }

            if (k > 0)
            {
                timer1.Start();
                errorIP = "";
                errorSwitch = 0;
                return;
            }

            if (errorSwitch == 0 && IP != SwitchIP48 && d.SelectedTab == MainTab_SPR)
            {
                errorIP = targetIP;
                errorSwitch = 1;
                SPR_PingStatusLebel.Text = "PING STATUS: NAPAKA!";
                SPR_PingStatusLebel.BackColor = Color.Red;
            }
            else if (errorSwitch == 0)
            {
                errorIP = targetIP;
                errorSwitch = 1;
                MessageBox.Show("IP naslov " + IP + " neodziven na ping!");
            }

        }
        //IZBIRA ERICSSON SPREJEMNIKA
        public void IzbranERICSSON()
        {
            targetIP = IzbranSPR.IP;
            
            if(pingTarget(IzbranSPR.IP) == false)
            {
                return;
            }

            //PREVERI ČE IMA SPREJEMNIK IP OPCIJO
            if (IzbranSPR.Tip == "SAT in IP")
            {
                //CURRENT IP-IN DATA
                IpIN1IPLabel.Text = ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.1773.1.3.208.2.7.1.30.1.16.1");
                IPIn1textBox.Text = IpIN1IPLabel.Text;
                IpIN1PortLabel.Text = "UDP Port: " + ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.1773.1.3.208.2.7.1.30.1.18.1");
                UDPPort1textBox.Text = ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.1773.1.3.208.2.7.1.30.1.18.1");

                //CURRENT INPUT PORT IN USE
                string responsePortInUse = ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.1773.1.3.208.2.7.2.1.0");
                if (responsePortInUse == "0")
                {
                    INPortLabel.Text = "Port 1";
                }
                else if (responsePortInUse == "1")
                {
                    INPortLabel.Text = "Port 2";
                }
                else
                {
                    INPortLabel.Text = responsePortInUse;
                }

                //VKLOPI IP OPCIJE
                VklopIP();
                EricssonMulticast();
            }

            else if(IzbranSPR.Tip == "IP ONLY")
            {
                //CURRENT IP-IN DATA
                IpIN1IPLabel.Text = ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.1773.1.3.208.2.5.1.30.1.16.1");
                IPIn1textBox.Text = IpIN1IPLabel.Text;
                IpIN1PortLabel.Text = "UDP Port: " + ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.1773.1.3.208.2.5.1.30.1.18.1");
                UDPPort1textBox.Text = ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.1773.1.3.208.2.5.1.30.1.18.1");

                //CURRENT INPUT PORT IN USE
                string responsePortInUse = ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.1773.1.3.208.2.5.2.1.0");
                if (responsePortInUse == "0")
                {
                    INPortLabel.Text = "Port 1";
                }
                else if (responsePortInUse == "1")
                {
                    INPortLabel.Text = "Port 2";
                }
                else
                {
                    INPortLabel.Text = responsePortInUse;
                }

                //VKLOPI IP OPCIJE
                VklopIP();
                EricssonMulticast();
            }

            else
            {
                IzberiInputcomboBox.Items.Clear();
                IzberiInputcomboBox.Items.Add("ASI");
                IzberiInputcomboBox.Items.Add("SAT");

                //IZKLOPI IP INFO IN OPCIJE
                IzklopIP();
            }

            //PREBERI SERVICE
            Services();
        }


        //IZBIRA ATEME SPREJEMNIKA
        public void IzbranATEME()
        {
            Sprejemniki1 Izbran = Sprejemniki1.FirstOrDefault(p => p.Ime == Sprejemniki.Text);

            targetIP = IzbranSPR.IP;

            if (pingTarget(IzbranSPR.IP) == false)
            {
                return;
            }

            //VKLOPI IP OPCIJE
            VklopIP();
            
            //CURRENT IP-IN DATA
            IpIN1IPLabel.Text = HexToDecIP(ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.27338.5.3.2.2.2.1.0"));
            IPIn1textBox.Text = IpIN1IPLabel.Text;
            IpIN1PortLabel.Text = "UDP Port: " + ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.27338.5.3.2.2.2.2.0");
            UDPPort1textBox.Text = ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.27338.5.3.2.2.2.2.0");

            //CURRENT INPUT PORT IN USE
            string responsePortInUse = ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.27338.5.3.2.2.2.3.0");
            if (responsePortInUse == "1")
            {
                INPortLabel.Text = "Port 1";
                if(IpIN1IPLabel.Text == "0.0.0.0")
                {
                    IPIn1textBox.Text = IpIN1IPLabel.Text;
                    IPIn1textBox.Enabled = false;
                }
            }
            else if (responsePortInUse == "2")
            {
                INPortLabel.Text = "Port 2";
                if (IpIN1IPLabel.Text == "0.0.0.0")
                {
                    IPIn1textBox.Text = IpIN1IPLabel.Text;
                    IPIn1textBox.Enabled = false;
                }
            }
            else
            {
                INPortLabel.Text = responsePortInUse;
            }
            
            if(IPIn1textBox.Text == "0.0.0.0")
            {
                AtemeMulticheckBox.Checked = false;

            }
            else
            {
                AtemeMulticheckBox.Checked = true;
            }

            //PREBERI SERVICE
            Services();
        }
        
        //IZBIRA DRUGIH PROIZVAJALCEV
        public void izbranDrugi()
        {
            Sprejemniki1 Izbran = Sprejemniki1.FirstOrDefault(p => p.Ime == Sprejemniki.Text);

            targetIP = IzbranSPR.IP;

            IzberiInputcomboBox.Items.Clear();
            IzberiInputcomboBox.Items.Add("IP");
            IpIN1IPLabel.Text = "";
            IpIN1PortLabel.Text = "";
            INPortLabel.Text = "";
            
            Vlan1comboBox.Enabled = true;
            IPIn1textBox.Text = "";
            IPIn1textBox.Enabled = false;
            UDPPort1textBox.Text = "";
            UDPPort1textBox.Enabled = false;
        }
        

        //VKLOP IP OPCIJE
        public void VklopIP()
        {
            nastaviIpIn1label.Text = "IP-IN:";
            IzberiInputcomboBox.Items.Clear();
            IzberiInputcomboBox.Items.Add("ASI");
            if(IzbranSPR.Tip != "IP ONLY")
            {
                IzberiInputcomboBox.Items.Add("SAT");
            }
            IzberiInputcomboBox.Items.Add("IP");
            IPIn1textBox.Enabled = true;
            UDPPort1textBox.Enabled = true;
            Vlan1comboBox.Enabled = true;

            //ATEME IZJEME
            if (IzbranSPR.Proizvajalec == "ateme")
            {
                nastaviIpIn1label.Text = "IP-IN:";
                IzberiInputcomboBox.Items.Add("ZIXI");
            }
        }

        //IZKLOP IP OPCIJE
        public void IzklopIP()
        {
            IpIN1IPLabel.Text = "";
            IpIN1PortLabel.Text = "";
            INPortLabel.Text = "";

            IPIn1textBox.Text = "";
            IPIn1textBox.Enabled = false;
            UDPPort1textBox.Text = "";
            UDPPort1textBox.Enabled = false;
            Vlan1comboBox.SelectedItem = 0;
            Vlan1comboBox.Enabled = false;
        }
        
        //PREVOD HEX IP NASLOVA V DEC
        public string HexToDecIP(string hexValues)
        {
            string[] hexValuesSplit = hexValues.Split(' ');
            string stringValue = "";
            foreach (String hex in hexValuesSplit)
            {
                // Convert the number expressed in base-16 to an integer.
                int value;
                try
                {
                    value = Convert.ToInt32(hex, 16);
                }
                catch
                {
                    return stringValue;
                }

                // Get the character corresponding to the integral value.
                if (stringValue == "")
                {
                    stringValue = Convert.ToString(value);
                }
                else
                {
                    stringValue = stringValue + "." + Convert.ToString(value);
                }
            }
            return stringValue;
        }

        
        //PREVOD DEC IP NASLOVA V HEX
        public byte[] DecToHexIP(string decValues)
        {
            string[] decValuesSplit = decValues.Split('.');
            byte[] byteValue = new byte[4] {0x00, 0x00, 0x00, 0x00};
            int i = 0;

            foreach (String dec in decValuesSplit)
            {
                try
                {
                    byteValue[i] = Convert.ToByte(dec);
                    i++;
                }
                catch
                {
                    byteValue[i] = 00;
                }
            }
            return byteValue;
        }

        
        //SNMP SET IPADRESS Ateme
        public void SNMPSetIPAdressAteme(string TargetIPAdress, string OID, byte[] buf)
        {
            if (pingTarget(TargetIPAdress) == false)
            {
                return;
            }
            
            UdpTarget target = new UdpTarget((IPAddress)new IpAddress(TargetIPAdress));

            // Create a SET PDU
            Pdu pdu = new Pdu(PduType.Set);

            //preveri ali je nastavljen multicast mode
            if (AtemeMulticheckBox.Checked == false)
            {
                byte[] uni = DecToHexIP("0.0.0.0");
                try
                {
                    pdu.VbList.Add(new Oid(OID), new OctetString(uni));
                }
                catch
                {
                    MessageBox.Show("IP naslov ni pravilno nastavljen!");
                }
            }

            else
            {
                try
                {
                    pdu.VbList.Add(new Oid(OID), new OctetString(buf));
                }
                catch
                {
                    MessageBox.Show("IP naslov ni pravilno nastavljen!");
                }
            }
            
            AgentParameters aparam = new AgentParameters(SnmpVersion.Ver2, new OctetString("private"));

            // Response packet
            SnmpV2Packet response;

            //pošlji SNMP SET ukaz
            response = target.Request(pdu, aparam) as SnmpV2Packet;
        }

        //SNMP SET UDP port Ateme
        public void SNMPSetUDPAteme(string TargetIPAdress, string OID, UInt32 UDP)
        {
            if (pingTarget(TargetIPAdress) == false)
            {
                return;
            }

            UdpTarget target = new UdpTarget((IPAddress)new IpAddress(TargetIPAdress));

            // Create a SET PDU
            Pdu pdu = new Pdu(PduType.Set);
            try
            {
                pdu.VbList.Add(new Oid(OID), new Gauge32(UDP));
            }
            catch
            {
                MessageBox.Show("UDP port ni pravilno nastavljen!");
            }

            AgentParameters aparam = new AgentParameters(SnmpVersion.Ver2, new OctetString("private"));

            // Response packet
            SnmpV2Packet response;

            //pošlji SNMP SET ukaz
            response = target.Request(pdu, aparam) as SnmpV2Packet;
        }
        
        //SNMP GET ZAHTEVEK
        public string ReadSNMP(string TargetIPAdress, string OID)
        {
            if(pingTarget(TargetIPAdress) == false )
            {
                return "Nedosegljiva naprava!";
            }

            OctetString community = new OctetString("private");
            AgentParameters param = new AgentParameters(community);
            param.Version = SnmpVersion.Ver2;
            IpAddress agent = new IpAddress(TargetIPAdress);
            if (!agent.Valid)
            {
                return "Napačen IP naslov!";
            }
            UdpTarget target = new UdpTarget((IPAddress)agent, 161, 1000, 3); //Parametri za timeout
            Pdu pdu = new Pdu(PduType.Get);
            pdu.VbList.Add(OID);
            try
            {
                SnmpV2Packet result = (SnmpV2Packet)target.Request(pdu, param);
                if (result != null)
                {
                    if (result.Pdu.ErrorStatus != 0)
                    {
                        return ("Error in SNMP reply.");
                    }

                    else if (result.Pdu.VbList[0].Value.ToString() == "SNMP No-Such-Object")
                    {
                        return ("NAPAKA");
                    }

                    else
                    {
                        return (result.Pdu.VbList[0].Value.ToString());
                    }
                }
                else
                {
                    return ("No response received from SNMP agent.");
                }
            }

            catch
            {
                return("Napaka pri pošiljanju snmp paketa!");
            }
        }

        //SNMP-1 GET ZAHTEVEK
        public string SNMP1_GET(string TargetIPAdress, string OID, string comm)
        {
            if (pingTarget(TargetIPAdress) == false)
            {
                return "Nedosegljiva naprava!";
            }

            OctetString community = new OctetString(comm);
            AgentParameters param = new AgentParameters(community);
            param.Version = SnmpVersion.Ver1;
            IpAddress agent = new IpAddress(TargetIPAdress);
            if (!agent.Valid)
            {
                return "Napačen IP naslov!";
            }
            UdpTarget target = new UdpTarget((IPAddress)agent, 161, 1000, 3); //Parametri za timeout
            Pdu pdu = new Pdu(PduType.Get);
            pdu.VbList.Add(OID);
            try
            {
                SnmpV1Packet result = (SnmpV1Packet)target.Request(pdu, param);
                if (result != null)
                {
                    if (result.Pdu.ErrorStatus != 0)
                    {
                        return ("Error in SNMP reply.");
                    }

                    else if (result.Pdu.VbList[0].Value.ToString() == "SNMP No-Such-Object")
                    {
                        return ("NAPAKA");
                    }

                    else
                    {
                        return (result.Pdu.VbList[0].Value.ToString());
                    }
                }
                else
                {
                    return ("No response received from SNMP agent.");
                }
            }

            catch
            {
                return ("Napaka pri pošiljanju snmp paketa!");
            }
        }

        //SNMP SET INT
        public void SNMPSet(string TargetIPAdress, string OID, int novOID)
        {
            if(pingTarget(TargetIPAdress) == false)
            {
                return;
            }

            UdpTarget target = new UdpTarget((IPAddress)new IpAddress(TargetIPAdress));

            // Create a SET PDU
            Pdu pdu = new Pdu(PduType.Set);
            pdu.VbList.Add(new Oid(OID), new Integer32(Convert.ToInt32(novOID)));
            AgentParameters aparam = new AgentParameters(SnmpVersion.Ver2, new OctetString("private"));

            // Response packet
            SnmpV2Packet response;

            //pošlji SNMP SET ukaz
            response = target.Request(pdu, aparam) as SnmpV2Packet;
        }

        //SNMP-1 SET INT
        public void SNMP1Set(string TargetIPAdress, string OID, int novOID)
        {
            if (pingTarget(TargetIPAdress) == false)
            {
                return;
            }

            UdpTarget target = new UdpTarget((IPAddress)new IpAddress(TargetIPAdress));

            // Create a SET PDU
            Pdu pdu = new Pdu(PduType.Set);
            pdu.VbList.Add(new Oid(OID), new Integer32(Convert.ToInt32(novOID)));
            AgentParameters aparam = new AgentParameters(SnmpVersion.Ver1, new OctetString("private"));

            // Response packet
            SnmpV2Packet response;

            //pošlji SNMP SET ukaz
            response = target.Request(pdu, aparam) as SnmpV2Packet;
        }

        //SNMP SET IPADRESS EERICSSON
        public void SNMPSetIPAdressE(string TargetIPAdress, string OID, string newIP)
        {
            if (pingTarget(TargetIPAdress) == false)
            {
                return;
            }

            UdpTarget target = new UdpTarget((IPAddress)new IpAddress(TargetIPAdress));

            // Create a SET PDU
            Pdu pdu = new Pdu(PduType.Set);
            try
            {
                pdu.VbList.Add(new Oid(OID), new IpAddress(newIP));
            }
            catch
            {
                MessageBox.Show("IP naslov ni pravilno nastavljen!");
            }
            
            AgentParameters aparam = new AgentParameters(SnmpVersion.Ver2, new OctetString("private"));

            // Response packet
            SnmpV2Packet response;

            //pošlji SNMP SET ukaz
            response = target.Request(pdu, aparam) as SnmpV2Packet;
        }


        //SERVICE IDENTIFICATION
        public string ServiceCheck()
        {
            if (IzbranSPR.Proizvajalec == "ericsson")
            {
                return ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.1773.1.3.208.4.1.2.0");
            }

            else if(IzbranSPR.Proizvajalec == "ateme")
            {
                return ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.27338.5.5.2.1.1.0");
            }
            else
            {
                return "NAPAKA";
            } 
        }
        
        //POLNJENJE SERVICE BOXA S SERVICI
        public void Services()
        {
            IzberiServicecomboBox.Items.Clear();
            int i = 1;

            if (IzbranSPR.Proizvajalec == "ericsson")
            {
                while (ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.1773.1.3.208.4.1.1.1.2." + i.ToString()) != "NAPAKA")
                {
                    IzberiServicecomboBox.Items.Add(ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.1773.1.3.208.4.1.1.1.2." + i.ToString()));
                    i++;
                }
                IzberiServicecomboBox.SelectedIndex = 0;
            }

            if (IzbranSPR.Proizvajalec == "ateme")
            {
                while (ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.27338.5.5.1.5.1.1.5." + i.ToString()) != "0" && ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.27338.5.5.1.5.1.1.5." + i.ToString()) != "NAPAKA")
                {
                    IzberiServicecomboBox.Items.Add(ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.27338.5.5.1.5.1.1.5." + i.ToString()));
                    i++;
                }
            }
        }

        //IZBERI NOV SERVICE
        private void IzberiServicebutton_Click(object sender, EventArgs e)
        {
            if (IzberiServicecomboBox.SelectedIndex == -1)
            {
                return;
            }

            if (IzbranSPR.Proizvajalec == "ericsson")
            {
                if (IzberiServicecomboBox.SelectedIndex == 0)
                {
                    return;
                }
                int EServiceINT = Convert.ToInt32(ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.1773.1.3.208.4.1.1.1.5." + Convert.ToString(IzberiServicecomboBox.SelectedIndex+1)));
                SNMPSet(IzbranSPR.IP, "1.3.6.1.4.1.1773.1.3.208.4.1.2.0", EServiceINT);
            }

            else if (IzbranSPR.Proizvajalec == "ateme")
            {
                int AServiceINT = Convert.ToInt32(IzberiServicecomboBox.Text);
                SNMPSet(IzbranSPR.IP, "1.3.6.1.4.1.27338.5.3.2.3.2.1.0", AServiceINT);
            }
        }

        //REFRESH STATISTIKE
        public void RefreshStats()
        {
            if (d.SelectedIndex == 1)
            {
                if(Ostale_Vmesnik_button.Text != "klik klik")
                {
                    ENC_PC_VLANlabel.Text = "VLAN: " + ReadSNMP(IZBRSwitch(IzbranENC.Switch), "1.3.6.1.4.1.9.9.68.1.2.2.1.2.101" + IzbranENC.Switchport);
                }
                return;
            }

            if (d.SelectedIndex == 0 && ImeSprejemnikaButton.Text != "klik klik")
            {
                if (IzbranSPR.Proizvajalec == "ericsson")
                {
                    if (ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.1773.1.3.208.2.1.7.0") == "0")
                    {
                        InputStatuslabel.Text = "NI STREAMA!";
                        ServiceStatuslabel.Text = "";
                        InputStatuslabel.ForeColor = Color.Red;
                    }
                    else if (ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.1773.1.3.208.2.1.7.0") == "1")
                    {
                        InputStatuslabel.Text = "SPREJEMAM!";
                        InputStatuslabel.ForeColor = Color.Green;
                        ServiceStatuslabel.Text = "Service: " + ServiceCheck();
                    }

                    //CURRENT INPUT
                    string responseINPUT = ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.1773.1.3.208.2.1.6.0");
                    if (responseINPUT == "0" && IzbranSPR.Tip != "IP ONLY")
                    {
                        CurrentInputLabel.Text = "ASI";
                    }
                    else if (responseINPUT == "1" && IzbranSPR.Tip != "IP ONLY")
                    {
                        CurrentInputLabel.Text = "SAT";
                    }
                    else if (responseINPUT == "2" && IzbranSPR.Tip != "IP ONLY")
                    {
                        CurrentInputLabel.Text = "IP";
                    }
                    else if (responseINPUT == "0" && IzbranSPR.Tip == "IP ONLY")
                    {
                        CurrentInputLabel.Text = "ASI";
                    }
                    
                    else if (responseINPUT == "1" && IzbranSPR.Tip == "IP ONLY")
                    {
                        CurrentInputLabel.Text = "IP";
                    }

                    else
                    {
                        CurrentInputLabel.Text = responseINPUT;
                    }

                    if (IzbranSPR.Tip == "SAT")
                    {
                        CurrFrekvencaLabel.Text = Hz2MHz(ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.1773.1.3.208.2.2.15.1.3.1"), 1) + " MHz";
                        CurrSymRateLabel.Text = Hz2MHz(ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.1773.1.3.208.2.2.15.1.4.1"), 3) + " kS/s";
                        CurrLNBLabel.Text = Hz2MHz(ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.1773.1.3.208.2.2.15.1.2.1"), 1) + " MHz";
                        CurLBMlabel.Text = "";
                    }
                    
                    //PREVERI ČE IMA SPREJEMNIK IP OPCIJO
                    if (IzbranSPR.Tip == "SAT in IP")
                    {
                        //CURRENT IP-IN DATA
                        IpIN1IPLabel.Text = ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.1773.1.3.208.2.7.1.30.1.16.1");
                        IpIN1PortLabel.Text = "UDP Port: " + ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.1773.1.3.208.2.7.1.30.1.18.1");
                        
                        //CURRENT INPUT PORT IN USE
                        string responsePortInUse = ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.1773.1.3.208.2.7.2.1.0");
                        if (responsePortInUse == "0")
                        {
                            INPortLabel.Text = "Port 1";
                        }
                        else if (responsePortInUse == "1")
                        {
                            INPortLabel.Text = "Port 2";
                        }
                        else
                        {
                            INPortLabel.Text = responsePortInUse;
                        }

                        //Refresh SAT stats
                        CurrFrekvencaLabel.Text = Hz2MHz(ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.1773.1.3.208.2.7.3.15.1.3.1"), 1) + " MHz";
                        CurrSymRateLabel.Text = Hz2MHz(ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.1773.1.3.208.2.7.3.15.1.4.1"), 3) + " kS/s";
                        CurrLNBLabel.Text = Hz2MHz(ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.1773.1.3.208.2.7.3.15.1.2.1"), 1) + " MHz";

                    }
                    if (IzbranSPR.Tip == "IP ONLY")
                    {
                        //CURRENT IP-IN DATA
                        IpIN1IPLabel.Text = ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.1773.1.3.208.2.5.1.30.1.16.1");
                        IpIN1PortLabel.Text = "UDP Port: " + ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.1773.1.3.208.2.5.1.30.1.18.1");

                        //CURRENT INPUT PORT IN USE
                        string responsePortInUse = ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.1773.1.3.208.2.5.2.1.0");
                        if (responsePortInUse == "0")
                        {
                            INPortLabel.Text = "Port 1";
                        }
                        else if (responsePortInUse == "1")
                        {
                            INPortLabel.Text = "Port 2";
                        }
                        else
                        {
                            INPortLabel.Text = responsePortInUse;
                        }
                    }
                }

                if (IzbranSPR.Proizvajalec == "ateme")
                {
                    if (ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.27338.5.5.3.6.0") == "2")
                    {
                        InputStatuslabel.Text = "NI STREAMA!";
                        InputStatuslabel.ForeColor = Color.Red;
                    }
                    else if (ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.27338.5.5.3.6.0") == "1")
                    {
                        InputStatuslabel.Text = "SPREJEMAM!";
                        InputStatuslabel.ForeColor = Color.Green;
                        ServiceStatuslabel.Text = ServiceCheck();
                    }

                    //CURRENT INPUT
                    string responseINPUT = ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.27338.5.5.3.2.0");
                    if (responseINPUT == "1")
                    {
                        CurrentInputLabel.Text = "IP";
                    }
                    else if (responseINPUT == "2")
                    {
                        CurrentInputLabel.Text = "ASI";
                    }
                    else if (responseINPUT == "3")
                    {
                        CurrentInputLabel.Text = "SAT";
                    }
                    else if (responseINPUT == "4")
                    {
                        CurrentInputLabel.Text = "DS";
                    }
                    else if (responseINPUT == "5")
                    {
                        CurrentInputLabel.Text = "ZIXI";
                    }
                    else
                    {
                        CurrentInputLabel.Text = responseINPUT;
                    }
                    
                    //CURRENT IP-IN DATA
                    IpIN1IPLabel.Text = HexToDecIP(ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.27338.5.3.2.2.2.1.0"));
                    IpIN1PortLabel.Text = "UDP Port: " + ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.27338.5.3.2.2.2.2.0");

                    //CURRENT INPUT PORT IN USE
                    string responsePortInUse = ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.27338.5.3.2.2.2.3.0");
                    if (responsePortInUse == "1")
                    {
                        INPortLabel.Text = "Port 1";
                    }
                    else if (responsePortInUse == "2")
                    {
                        INPortLabel.Text = "Port 2";
                    }
                    else
                    {
                        INPortLabel.Text = responsePortInUse;
                    }
                    
                    //Refresh SAT stats
                    CurrFrekvencaLabel.Text = Hz2MHz(ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.27338.5.3.2.2.4.5.0"), 1) + " MHz";
                    CurrSymRateLabel.Text = atemeSymRate(ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.27338.5.3.2.2.4.3.0"), 3) + " kS/s";
                    CurrLNBLabel.Text = Hz2MHz(ReadSNMP(IzbranSPR.IP, "1.3.6.1.4.1.27338.5.3.2.2.4.6.0"), 1) + " MHz";
                }

                //REFRESH VLANS
                if (IzbranSPR.Tip == "SAT in IP" || IzbranSPR.Tip == "IP ONLY")
                {
                    IpIN1VLANLabel.Text = "VLAN: " + ReadSNMP(IZBRSwitch(IzbranSPR.Switch), "1.3.6.1.4.1.9.9.68.1.2.2.1.2.101" + IzbranSPR.SwitchPort1);
                }
                else
                {
                    IpIN1VLANLabel.Text = "";
                }

                //refresh BISS okna
                try
                {
                    BISS B = BISS_data.FirstOrDefault(p => p.Sprejemnik == ImeSprejemnikaButton.Text);

                    BISSStatuslabel.Text = "BISS: " + B.Koda;
                }
                catch
                {
                    BISSStatuslabel.Text = "";
                }

                //Refresh LBM
                if (IzbranSPR.LMBPort != "0" && TipSprejemnikaLabel.Text != "")
                {
                    try
                    {
                        string n2 = SNMP1_GET(LBM_matrix_IP, "1.3.6.1.4.1.6827.50.30.2.2.1.3." + IzbranSPR.LMBPort, "public");
                        LBM c2 = LBM_data.FirstOrDefault(p2 => p2.InputPort == n2);
                        CurLBMlabel.Text = c2.Ime;
                    }
                    catch
                    {
                        CurLBMlabel.Text = "";
                    }
                }
            }

            
        }

        //INPUT STATUS REFRESH
        private void timer1_Tick(object sender, EventArgs e)
        {
            RefreshStats();
        }
        
        //MULTICAST CHECKBOX MAGIC
        public void multiCheck()
        {
            if (AtemeMulticheckBox.Checked == true)
            {
                IPIn1textBox.Enabled = true;
            }

            else if (AtemeMulticheckBox.Checked != true)
            {
                IPIn1textBox.Enabled = false;
            }
        }
        
        private void AtemeMulticheckBox_CheckedChanged(object sender, EventArgs e)
        {
            multiCheck();
        }

        //UNICAST - MULTICAST UPRAVLJANJE Z ERICSSONI
        public void EricssonMulticast()
        {
            if (IzbranSPR.Tip == "IP ONLY")
            {
                if (Convert.ToInt32(ReadSNMP(targetIP, "1.3.6.1.4.1.1773.1.3.208.2.5.1.30.1.21.1")) == 0)
                {
                    AtemeMulticheckBox.Checked = true;
                }
                else
                {
                    AtemeMulticheckBox.Checked = false;
                }
            }
            else
            {
                if (Convert.ToInt32(ReadSNMP(targetIP, "1.3.6.1.4.1.1773.1.3.208.2.7.1.30.1.21.1")) == 0)
                {
                    AtemeMulticheckBox.Checked = true;
                }
                else
                {
                    AtemeMulticheckBox.Checked = false;
                }
            }
            
            multiCheck();
        }

        //NASTAVI NOVE IP NASTAVITVE
        private void NastaviNovobutton_Click(object sender, EventArgs e)
        {
            if(ImeSprejemnikaButton.Text == "klik klik")
            {
                return;
            }

            if (TipSprejemnikaLabel.Text == "ericsson SAT in IP")
            {
                //UNI PRENOS
                if (AtemeMulticheckBox.Checked == false)
                {
                    SNMPSet(targetIP, "1.3.6.1.4.1.1773.1.3.208.2.7.1.30.1.21.1", 1);
                }
                else
                {
                    //MULTI PRENOS
                    if (IPIn1textBox.Text != "")
                    {
                        SNMPSet(targetIP, "1.3.6.1.4.1.1773.1.3.208.2.7.1.30.1.21.1", 0);
                        SNMPSetIPAdressE(targetIP, "1.3.6.1.4.1.1773.1.3.208.2.7.1.30.1.16.1", IPIn1textBox.Text);
                    }
                }
                
                try
                {
                    if(UDPPort1textBox.Text != "")
                    {
                        SNMPSet(targetIP, "1.3.6.1.4.1.1773.1.3.208.2.7.1.30.1.18.1", Convert.ToInt32(UDPPort1textBox.Text));
                    }
                    
                }
                catch
                {
                    MessageBox.Show("UDP port je nastavljen narobe!");
                }

                //vedno nastavi na IP-IN 1
                SNMPSet(targetIP, "1.3.6.1.4.1.1773.1.3.208.2.7.2.1.0", 0);
            }

            else if (TipSprejemnikaLabel.Text == "ericsson IP ONLY")
            {
                //UNI PRENOS
                if (AtemeMulticheckBox.Checked == false)
                {
                    SNMPSet(targetIP, "1.3.6.1.4.1.1773.1.3.208.2.5.1.30.1.21.1", 1);
                }
                else
                {
                    //MULTI PRENOS
                    SNMPSet(targetIP, "1.3.6.1.4.1.1773.1.3.208.2.5.1.30.1.21.1", 0);
                    SNMPSetIPAdressE(targetIP, "1.3.6.1.4.1.1773.1.3.208.2.5.1.30.1.16.1", IPIn1textBox.Text);
                }

                try
                {
                    SNMPSet(targetIP, "1.3.6.1.4.1.1773.1.3.208.2.5.1.30.1.18.1", Convert.ToInt32(UDPPort1textBox.Text));
                }
                catch
                {
                    MessageBox.Show("UDP port je nastavljen narobe!");
                }

                //Vedno IP-IN 1
                SNMPSet(targetIP, "1.3.6.1.4.1.1773.1.3.208.2.5.2.1.0", 0);
            }

            else if(TipSprejemnikaLabel.Text == "ateme SAT in IP")
            {
                byte[] HexIP = DecToHexIP(IPIn1textBox.Text);
                SNMPSetIPAdressAteme(targetIP, "1.3.6.1.4.1.27338.5.3.2.2.2.1.0", HexIP);
                UInt32 newUDP = Convert.ToUInt32(UDPPort1textBox.Text);
                SNMPSetUDPAteme(targetIP, "1.3.6.1.4.1.27338.5.3.2.2.2.2.0", newUDP);

                //vedno IP_IN 1
                SNMPSet(targetIP, "1.3.6.1.4.1.27338.5.3.2.2.2.3.0", 1);
            }

            if (Vlan1comboBox.SelectedIndex != -1)
            {
                SetVLAN(ImeSprejemnikaButton.Text, Vlan1comboBox.Text);
            }

            Vlan1comboBox.SelectedIndex = -1;
            serviceRefreshtimer.Start();
        }
        
        //Iskanje switcha po bazi
        public string IZBRSwitch(string ImeSwitcha)
        {
            Izbranswitch = Switch_data.FirstOrDefault(p => p.Ime == ImeSwitcha);
            return Izbranswitch.IP;
        }

        //nastavljanje VLANA na SWITCHU - sprejemniki
        public void SetVLAN(string naprava, string NewVLAN)
        {
            string[] VlanIme = NewVLAN.Split('-');

            VLANs vl1 = VLANs_data.FirstOrDefault(p => p.Ime == VlanIme[1].Remove(0, 1));
            
            Sprejemniki1 s2 = Sprejemniki1.FirstOrDefault(p => p.Ime == naprava);

            if (Vlan1comboBox.SelectedIndex != -1)
            {
                SNMPSet(IZBRSwitch(IzbranSPR.Switch), "1.3.6.1.4.1.9.9.68.1.2.2.1.2.101" + s2.SwitchPort1, Convert.ToInt32(vl1.Stevilo));
            }
        }

        //nastavljanje VLANA na SWITCHU - enc
        public void SetVLAN_ENC(string naprava, string NewVLAN)
        {
            string[] VlanIme = NewVLAN.Split('-');

            VLANs vl1 = VLANs_data.FirstOrDefault(p => p.Ime == VlanIme[1].Remove(0, 1));

            Enkoderji s2 = ENC_data.FirstOrDefault(p => p.Ime == naprava);

            if (VLAN_ENC_PCcomboBox.SelectedIndex != -1)
            {
                SNMPSet(IZBRSwitch(IzbranENC.Switch), "1.3.6.1.4.1.9.9.68.1.2.2.1.2.101" + s2.Switchport, Convert.ToInt32(vl1.Stevilo));
            }
        }

        //GUBM NASTAVI INPUT
        private void button1_Click(object sender, EventArgs e)
        {

            Sprejemniki1 SpIN = Sprejemniki1.FirstOrDefault(p => p.Ime == ImeSprejemnikaButton.Text);

            if (IzberiInputcomboBox.SelectedIndex == 0)
            {
                if(TipSprejemnikaLabel.Text == "ericsson SAT")
                {
                    SNMPSet(targetIP, "1.3.6.1.4.1.1773.1.3.208.2.1.2.0", 0);
                }

                else if (TipSprejemnikaLabel.Text == "ericsson SAT in IP")
                {
                    SNMPSet(targetIP, "1.3.6.1.4.1.1773.1.3.208.2.1.2.0", 0);
                }

                else if (SpIN.Tip == "IP ONLY")
                {
                    SNMPSet(targetIP, "1.3.6.1.4.1.1773.1.3.208.2.1.2.0", 0);
                }

                else if (TipSprejemnikaLabel.Text == "ateme SAT in IP")
                {
                    SNMPSet(targetIP, "1.3.6.1.4.1.27338.5.3.2.2.1.0", 2);
                }
            }

            else if (IzberiInputcomboBox.SelectedIndex == 1)
            {
                if (TipSprejemnikaLabel.Text == "ericsson SAT")
                {
                    SNMPSet(targetIP, "1.3.6.1.4.1.1773.1.3.208.2.1.2.0", 1);
                }

                else if (TipSprejemnikaLabel.Text == "ericsson SAT in IP")
                {
                    SNMPSet(targetIP, "1.3.6.1.4.1.1773.1.3.208.2.1.2.0", 1);
                }

                else if (SpIN.Tip == "IP ONLY")
                {
                    SNMPSet(targetIP, "1.3.6.1.4.1.1773.1.3.208.2.1.2.0", 1);
                }

                else if (TipSprejemnikaLabel.Text == "ateme SAT in IP")
                {
                    SNMPSet(targetIP, "1.3.6.1.4.1.27338.5.3.2.2.1.0", 3);
                }
            }

            else if (IzberiInputcomboBox.SelectedIndex == 2)
            {
                if (TipSprejemnikaLabel.Text == "ericsson SAT in IP" && SpIN.Tip != "IP ONLY")
                {
                    SNMPSet(targetIP, "1.3.6.1.4.1.1773.1.3.208.2.1.2.0", 2);
                }

                else if (TipSprejemnikaLabel.Text == "ateme SAT in IP")
                {
                    SNMPSet(targetIP, "1.3.6.1.4.1.27338.5.3.2.2.1.0", 1);
                }
            }
            
            else if (IzberiInputcomboBox.SelectedIndex == 3)
            {
                if (TipSprejemnikaLabel.Text == "ateme SAT in IP")
                {
                    SNMPSet(targetIP, "1.3.6.1.4.1.27338.5.3.2.2.1.0", 5);
                }
            }

            Services();
            serviceRefreshtimer.Start();
        }

        private void serviceRefreshtimer_Tick(object sender, EventArgs e)
        {
            Services();
            serviceRefreshtimer.Stop();
        }
        
        //nastavi novo BISS kodo
        private void BISSbutton_Click(object sender, EventArgs e)
        {
            Sprejemniki1 SpBiss = Sprejemniki1.FirstOrDefault(p => p.Ime == ImeSprejemnikaButton.Text);

            byte[] HEXBISS = BISStoHex(BISStextBox.Text);
            if (SpBiss.Proizvajalec == "ericsson")
            {
                SetBISS(targetIP, "1.3.6.1.4.1.1773.1.3.208.8.3.1.0", "1.3.6.1.4.1.1773.1.3.208.8.3.2.0", HEXBISS);
            }

            if(SpBiss.Proizvajalec == "ateme")
            {
                //nastavi
                SNMPSet(targetIP, "1.3.6.1.4.1.27338.5.3.2.3.7.1.0", 2);
                SetBISS(targetIP, "1.3.6.1.4.1.27338.5.3.2.3.7.3.1.0", "1.3.6.1.4.1.27338.5.3.2.3.7.3.2.0", HEXBISS);
            }

            //posodobi list
            BISS B1 = BISS_data.FirstOrDefault(p => p.Sprejemnik == ImeSprejemnikaButton.Text);
            string old = B1.Sprejemnik + "," + B1.Koda;
            if(BISStextBox.Text == null)
            {
                B1.Koda = "0";
            }
            else
            {
                B1.Koda = BISStextBox.Text;
            }
            string nov = B1.Sprejemnik + "," + B1.Koda;
            
            string text = File.ReadAllText(bp.BISS_CSP_Baza_path);
            text = text.Replace(old, nov);
            File.WriteAllText(bp.BISS_CSP_Baza_path, text);
            
            BISStextBox.Clear();
            
        }

        //PREVOD BISS V HEX
        public byte[] BISStoHex(string BISS)
        {
            byte[] BISS1Value = new byte[12] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            byte[] BISSEValue = new byte[16] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            int i = 0;
            
            if (BISS.Length == 12)
            {
                foreach (char x in BISS)
                {
                    try
                    {
                        BISS1Value[i] = Convert.ToByte(x);
                        i++;
                    }
                    catch
                    {
                        BISS1Value[i] = 00;
                    }
                }
                return BISS1Value;
            }

            else
            {
                foreach (char x in BISS)
                {
                    try
                    {
                        BISSEValue[i] = Convert.ToByte(x);
                        i++;
                    }
                    catch
                    {
                        BISSEValue[i] = 00;
                    }
                }
                return BISSEValue;
            }
        }

        //SNMP SET BISS
        public void SetBISS(string TargetIPAdress, string OIDBISSType, string OIDBISScode, byte[] buf)
        {
            if (pingTarget(TargetIPAdress) == false)
            {
                return;
            }
            
            UdpTarget target = new UdpTarget((IPAddress)new IpAddress(TargetIPAdress));

            // Create a SET PDU
            Pdu pdu = new Pdu(PduType.Set);

            if(BISStextBox.TextLength == 12 && IzbranSPR.Proizvajalec == "ericsson")
            {
                //nastavi biss 1
                SNMPSet(TargetIPAdress, OIDBISSType, 0);
            }

            else if (BISStextBox.TextLength == 12 && IzbranSPR.Proizvajalec == "ateme")
            {
                //nastavi biss 1
                SNMPSet(TargetIPAdress, OIDBISSType, 1);
            }

            else if (BISStextBox.TextLength == 16 && IzbranSPR.Proizvajalec == "ericsson")
            {
                //nastavi biss E
                SNMPSet(TargetIPAdress, OIDBISSType, 3);
            }

            else if (BISStextBox.TextLength == 16 && IzbranSPR.Proizvajalec == "ateme")
            {
                //nastavi biss E
                SNMPSet(TargetIPAdress, OIDBISSType, 2);
                OIDBISScode = "1.3.6.1.4.1.27338.5.3.2.3.5.3.0";
            }
            
            else
            {
                MessageBox.Show("Nepravilno vnešena BISS koda! (12 znakov za BISS-1, 16 za BISS-E");
            }

            pdu.VbList.Add(new Oid(OIDBISScode), new OctetString(buf));
            AgentParameters aparam = new AgentParameters(SnmpVersion.Ver2, new OctetString("private"));

            // Response packet
            SnmpV2Packet response;

            //pošlji SNMP SET ukaz
            response = target.Request(pdu, aparam) as SnmpV2Packet;
        }

        private void RePingtimer_Tick(object sender, EventArgs e)
        {
            REpingTarget(targetIP);
            RePingtimer.Stop();
        }
        
        //NASTAVLJANJE VLANOV ENKODERJEV in pcjev
        private void NastaviENC_Click(object sender, EventArgs e)
        {
            SetVLAN_ENC(izbranENCPClabel.Text, VLAN_ENC_PCcomboBox.Text);
        }

        //Odpri Web Vmesnik
        public void WebVmesnik()
        {
            System.Diagnostics.Process.Start("http://" + targetIP);
        }
        
        private void ImeSprejemnikaButton_Click(object sender, EventArgs e)
        {
            if(IzbranSPR.Proizvajalec == "aviwest")
            {
                System.Diagnostics.Process.Start("http://" + IzbranSPR.IP +":8888");
                return;
            }
            WebVmesnik();
        }
        
        //Izbira multicast preseta
        private void MultiPresetSelection(object sender, EventArgs e)
        {
            if(IPIn1textBox.Enabled == false)
            {
                return;
            }

            int i = MultiPresetsComboBox.SelectedIndex;
            if(i == -1)
            {
                return;
            }

            Multicasts IzbranMC = Multi_data.FirstOrDefault(p => p.Ime == MultiPresetsComboBox.Text);
            VLANs vlan = VLANs_data.FirstOrDefault(p => p.Stevilo == IzbranMC.VLAN);

            IPIn1textBox.Text = IzbranMC.IP;
            UDPPort1textBox.Text = IzbranMC.UDP;
            string VLANpreset = IzbranMC.VLAN;

            if (VLANpreset == "0")
            {
                Vlan1comboBox.SelectedIndex = -1;
                return;
            }

             Vlan1comboBox.SelectedItem = IzbranMC.VLAN + " - " + vlan.Ime;
        }

        public string Hz2MHz(string Hz, int DecimalnihMest)
        {
            try
            {
                string freq = Convert.ToString(Math.Round(Convert.ToDouble(Hz) / 1000, DecimalnihMest));
                if (Hz.Length > 3)
                {
                    return freq;
                }
            }
            catch
            {
                return "error";
            }
            
            return "error";
        }

        public string atemeSymRate(string Hz, int DecimalnihMest)
        {
            try
            {
                string freq = Convert.ToString(Math.Round(Convert.ToDouble(Hz), DecimalnihMest));

                if (Hz.Length > 3)
                {
                    return freq;
                }
            }
            catch
            {
                return "error";
            }
            
            return "error";
        }

        //NastavI SAT parametre
        private void NastaviSATButton_Click(object sender, EventArgs e)
        {
            //popravi pogosto napako pri vpisu
            FrekvencaTextBox.Text = FrekvencaTextBox.Text.Replace('.', ',');
            SymRateTextBox.Text = SymRateTextBox.Text.Replace('.', ',');

            if (TipSprejemnikaLabel.Text == "ericsson SAT")
            {
                //nastavi na prvi SAT IN
                SNMPSet(targetIP, "1.3.6.1.4.1.1773.1.3.208.2.2.1.0", 0);
                
                if (FrekvencaTextBox.Text != "")
                {
                    SetSat("1.3.6.1.4.1.1773.1.3.208.2.2.15.1.3.1", "Frekvenca", 1000, FrekvencaTextBox);
                }
                if (SymRateTextBox.Text != "")
                {
                    SetSat("1.3.6.1.4.1.1773.1.3.208.2.2.15.1.4.1", "Symbol Rate", 1000, SymRateTextBox);
                }
                if (LNBTextBox.Text != "")
                {
                    SetSat("1.3.6.1.4.1.1773.1.3.208.2.2.15.1.2.1", "LNB", 1000, LNBTextBox);
                }
            }

            if (TipSprejemnikaLabel.Text == "ericsson SAT in IP")
            {
                //nastavi na prvi SAT IN
                SNMPSet(targetIP, "1.3.6.1.4.1.1773.1.3.208.2.7.3.1.0", 0);

                if(FrekvencaTextBox.Text != "")
                {
                    SetSat("1.3.6.1.4.1.1773.1.3.208.2.7.3.15.1.3.1", "Frekvenca", 1000, FrekvencaTextBox);
                }
                if (SymRateTextBox.Text != "")
                {
                    SetSat("1.3.6.1.4.1.1773.1.3.208.2.7.3.15.1.4.1", "Symbol Rate", 1000, SymRateTextBox);
                }
                if (LNBTextBox.Text != "")
                {
                    SetSat("1.3.6.1.4.1.1773.1.3.208.2.7.3.15.1.2.1", "LNB", 1000, LNBTextBox);
                }
            }

            if (TipSprejemnikaLabel.Text == "ateme SAT in IP")
            {
                //nastavi na RF1
                SNMPSet(targetIP, "1.3.6.1.4.1.27338.5.3.2.2.4.1.0", 1);

                if (FrekvencaTextBox.Text != "")
                {
                    SetSat("1.3.6.1.4.1.27338.5.3.2.2.4.5.0", "Frekvenca", 1000, FrekvencaTextBox);
                }
                if (SymRateTextBox.Text != "")
                {
                    SetSat("1.3.6.1.4.1.27338.5.3.2.2.4.3.0", "Symbol Rate", 1, SymRateTextBox);
                }
                if (LNBTextBox.Text != "")
                {
                    SetSat("1.3.6.1.4.1.27338.5.3.2.2.4.6.0", "LNB", 1000, LNBTextBox);
                }
            }

            //nastavi LBM input
            if (IzbranSPR.Tip != "IP ONLY" && LBMcomboBox.SelectedIndex != -1)
            {
                LBM c = LBM_data.FirstOrDefault(p => p.Ime == LBMcomboBox.Text);
                SNMP1Set(LBM_matrix_IP, "1.3.6.1.4.1.6827.50.30.2.2.1.3." + IzbranSPR.LMBPort, Convert.ToInt32(c.InputPort));
                LBMcomboBox.SelectedIndex = -1;
            }
        }

        public void SetSat(string OID, string KAJ, int rate, Control textbox)
        {
            try
            {
                SNMPSet(targetIP, OID, Convert.ToInt32(Convert.ToDouble(textbox.Text) * rate));
            }
            catch
            {
                MessageBox.Show(KAJ + " ni ok!");
            }
        }

        private void Nastav_Dodaj_Uredi_button_Click(object sender, EventArgs e)
        {
            if (Nastav_Dodaj_Uredi_button.Text == "DODAJANJE SPREJEMNIKOV")
            {
                SprNastav_cBox.Enabled = false;
                Nastav_IME_textBox.Text = "Novo Ime";
                Nastav_IP_textBox.Text = "Nov IP";
                Nastav_SPort1_textBox.Text = "Nov Port 1";
                Nastav_Dodaj_Uredi_button.Text = "UREJANJE SPREJEMNIKOV";
                Nastav_Brisanje_SPR_button.Visible = false;
                Nastav_Urejanje_SPR_button.Text = "DODAJ";
                return;
            }

            if (Nastav_Dodaj_Uredi_button.Text == "UREJANJE SPREJEMNIKOV")
            {
                SprNastav_cBox.Enabled = true;
                Nastav_IME_textBox.Text = "";
                Nastav_IP_textBox.Text = "";
                Nastav_SPort1_textBox.Text = "";
                Nastav_Dodaj_Uredi_button.Text = "DODAJANJE SPREJEMNIKOV";
                Nastav_Brisanje_SPR_button.Visible = true;
                Nastav_Urejanje_SPR_button.Text = "UREDI";
            }
        }

        private void NASTAV_SPRcBox_indexchange(object sender, EventArgs e)
        {
            Sprejemniki1 S4 = Sprejemniki1.FirstOrDefault(c => c.Ime == SprNastav_cBox.Text);

            int k = SprNastav_cBox.SelectedIndex;
            Nastav_SPR_IME_label.Text = S4.Ime;
            Nastav_SPR_Proiz_label.Text = S4.Proizvajalec;
            Nastav_SPR_IP_label.Text = S4.IP;
            Nastav_SPR_Switch_label.Text = S4.Switch;
            Nastav_SPR_SPort1_label.Text = S4.SwitchPort1;
            Nastav_SPR_Tip_label.Text = S4.Tip;
            Nastav_LBM_Port_label.Text = S4.LMBPort;

            Nastav_Brisanje_SPR_button.Visible = true;
        }
        
        //GUMB ZA UREJANJE ALI DODAJANJE NOVIH SPREJEMNIKOV
        private void Nastav_Urejanje_SPR_button_Click(object sender, EventArgs e)
        {
            //UREJANJE BAZE SPREJEMNIKOV
            if (SprNastav_cBox.SelectedIndex != -1 && Nastav_Urejanje_SPR_button.Text == "UREDI")
            {
                SidebarTimer.Stop();

                Sprejemniki1 S1 = Sprejemniki1.FirstOrDefault(p => p.Ime == SprNastav_cBox.Text);

                int k1 = SprNastav_cBox.SelectedIndex;

                if (Nastav_IME_textBox.Text != "")
                {
                    //posodobi BISS list z novim imenom
                    BISS B1 = BISS_data.FirstOrDefault(p => p.Sprejemnik == SprNastav_cBox.Text);
                    string old = B1.Sprejemnik + "," + B1.Koda;
                    B1.Sprejemnik = Nastav_IME_textBox.Text;
                    string nov = B1.Sprejemnik + "," + B1.Koda;
                    string text = File.ReadAllText(bp.BISS_CSP_Baza_path);
                    text = text.Replace(old, nov);
                    File.WriteAllText(bp.BISS_CSP_Baza_path, text);

                    //posodobi IME sprejemnika
                    Label myControl = Controls.Find(S1.Ime, true).FirstOrDefault() as Label;
                    S1.Ime = Nastav_IME_textBox.Text;
                    Nastav_SPR_IME_label.Text = S1.Ime;
                    myControl.Name = S1.Ime;

                    Nastav_IME_textBox.Text = "";
                }

                if (Nastav_Proiz_cBox.SelectedIndex != -1)
                {
                    S1.Proizvajalec = Nastav_Proiz_cBox.Text;
                    Nastav_SPR_Proiz_label.Text = S1.Proizvajalec;
                }
                
                if (Nastav_IP_textBox.Text != "")
                {
                    if(ValidateIPv4(Nastav_IP_textBox.Text) == true)
                    {
                        S1.IP = Nastav_IP_textBox.Text;
                        Nastav_SPR_IP_label.Text = S1.IP;
                    }
                    else
                    {
                        S1.IP = "0.0.0.0";
                        Nastav_SPR_IP_label.Text = S1.IP;
                    }
                    Nastav_IP_textBox.Text = "";
                }

                if(Nastav_SPR_Switch_comboBox.SelectedIndex != -1)
                {
                    S1.Switch = Nastav_SPR_Switch_comboBox.Text;
                }
                Nastav_SPR_Switch_label.Text = S1.Switch;

                if (Nastav_SPort1_textBox.Text != "")
                {
                    if (Nastav_SPort1_textBox.Text.Length == 1 && Nastav_SPort1_textBox.Text != "0")
                    {
                        S1.SwitchPort1 = "0" + Nastav_SPort1_textBox.Text;
                        S1.Switchport2 = "0" + Nastav_SPort1_textBox.Text;
                    }
                    else
                    {
                        S1.SwitchPort1 = Nastav_SPort1_textBox.Text;
                        S1.Switchport2 = Nastav_SPort1_textBox.Text;
                    }
                    Nastav_SPR_SPort1_label.Text = S1.SwitchPort1;
                    Nastav_SPort1_textBox.Text = "";
                }

                if (Nastav_TIP_SPR_cBox.SelectedIndex != -1)
                {
                    S1.Tip = Nastav_TIP_SPR_cBox.Text;
                    Nastav_SPR_Tip_label.Text = S1.Tip;
                }

                //OID
                string mix = S1.Proizvajalec + " " + S1.Tip;
                try
                {
                    OID O1 = OID_data.FirstOrDefault(p => p.Ime == mix);
                    S1.SigNoiseOID = O1.SigNoiseOID;
                    S1.LockedOID = O1.LockedOID;
                }
                catch
                {
                    S1.SigNoiseOID = "0";
                    S1.LockedOID = "0";
                }

                Nastav_Proiz_cBox.SelectedIndex = -1;
                Nastav_TIP_SPR_cBox.SelectedIndex = -1;

                if (Nastav_LBM_Port_textBox.Text != "" && Convert.ToInt16(Nastav_LBM_Port_textBox.Text) <= 16)
                {
                    try
                    {
                        Convert.ToInt16(Nastav_LBM_Port_textBox.Text);
                        S1.LMBPort = Nastav_LBM_Port_textBox.Text;
                        Nastav_LBM_Port_label.Text = S1.LMBPort;
                        Nastav_LBM_Port_textBox.Text = "";
                    }
                    catch
                    {
                        Nastav_LBM_Port_textBox.Text = "Napaka!";
                    }
                }
                
                //POSODOBI BAZO SPREJEMNIKOV IN COMBOBOXE
                using (TextWriter tw = new StreamWriter(bp.Sprejemniki_CSP_Baza_path))
                {
                    Sprejemniki.Items.Clear();
                    SprNastav_cBox.Items.Clear();
                    
                    foreach (Sprejemniki1 x in Sprejemniki1.OrderBy(c => c.Ime, new NaturalSortComparer<string>()))
                    {
                        tw.WriteLine(x.Ime + "," + x.Proizvajalec + "," + x.IP + "," + x.SwitchPort1 + "," + x.Switchport2
                             + "," + x.Tip + "," + x.LMBPort + "," + x.SigNoiseOID + "," + x.LockedOID + "," + x.Switch);

                        Sprejemniki.Items.Add(x.Ime);
                        SprNastav_cBox.Items.Add(x.Ime);
                    }
                }
                
                SidebarTimer.Start();
            }

            //dodajanje v bazo spejemnikov
            if (Nastav_Urejanje_SPR_button.Text == "DODAJ")
            {
                Sprejemniki1 novS = new Sprejemniki1();

                if(Nastav_SPR_Switch_comboBox.SelectedIndex == -1)
                {
                    MessageBox.Show("Izberi switch!", "Napaka!");
                    return;
                }

                if (Nastav_SPR_IME_label.Text != "")
                {
                    SidebarTimer.Stop();

                    //IME
                    novS.Ime = Nastav_IME_textBox.Text;
                    Nastav_IME_textBox.Text = "";

                    //firma
                    novS.Proizvajalec = Nastav_Proiz_cBox.Text;

                    //OID
                    string mix = Nastav_Proiz_cBox.Text + " " + Nastav_TIP_SPR_cBox.Text;
                    try
                    {
                        OID O1 = OID_data.FirstOrDefault(p => p.Ime == mix);
                        novS.SigNoiseOID = O1.SigNoiseOID;
                        novS.LockedOID = O1.LockedOID;
                    }
                    catch
                    {
                        novS.SigNoiseOID = "0";
                        novS.LockedOID = "0";
                    }

                    //tip sprejemnika
                    novS.Tip = Nastav_TIP_SPR_cBox.Text;
                    Nastav_TIP_SPR_cBox.SelectedIndex = -1;
                    Nastav_Proiz_cBox.SelectedIndex = -1;

                    //IP naslov
                    if (ValidateIPv4(Nastav_IP_textBox.Text) == true)
                    {
                        novS.IP = Nastav_IP_textBox.Text;
                        Nastav_IP_textBox.Text = "";
                    }
                    else
                    {
                        novS.IP = "0.0.0.0";
                        Nastav_IP_textBox.Text = "";
                    }

                    novS.Switch = Nastav_SPR_Switch_comboBox.Text;

                    //Port 1 - preveri veljavnost!
                    novS.SwitchPort1 = Nastav_SPort1_textBox.Text;
                    //Port 2
                    novS.Switchport2 = Nastav_SPort1_textBox.Text;
                    Nastav_SPort1_textBox.Text = "";
                    
                    //LBM port
                    novS.LMBPort = Nastav_LBM_Port_textBox.Text;
                    Nastav_LBM_Port_textBox.Text = "";

                    //Doda v BISS tabelo
                    BISS data = new BISS();
                    data.Sprejemnik = novS.Ime;
                    data.Koda = "0";
                    BISS_data.Add(data);

                    using (StreamWriter sw = File.AppendText(bp.BISS_CSP_Baza_path))
                    {
                        BISS nb = BISS_data.FirstOrDefault(c => c.Sprejemnik == novS.Ime);
                        sw.WriteLine(nb.Sprejemnik + "," + nb.Koda);
                    }

                    //doda nov class
                    Sprejemniki1.Add(novS);
                    
                    //POSODOBI BAZO SPREJEMNIKOV IN COMBOBOXE
                    using (TextWriter tw = new StreamWriter(bp.Sprejemniki_CSP_Baza_path))
                    {
                        Sprejemniki.Items.Clear();
                        SprNastav_cBox.Items.Clear();

                        foreach (Sprejemniki1 x in Sprejemniki1.OrderBy(c => c.Ime, new NaturalSortComparer<string>()))
                        {
                            tw.WriteLine(x.Ime + "," + x.Proizvajalec + "," + x.IP + "," + x.SwitchPort1 + "," + x.Switchport2
                                 + "," + x.Tip + "," + x.LMBPort + "," + x.SigNoiseOID + "," + x.LockedOID + "," + x.Switch);

                            Sprejemniki.Items.Add(x.Ime);
                            SprNastav_cBox.Items.Add(x.Ime);
                        }
                    }
                    
                    //SidebarTimer.Start();

                    MessageBox.Show("Sprejemnik uspešno dodan!", "JUHEJ!");
                }
            }
        }

        private void Nastav_Brisanje_SPR_button_Click(object sender, EventArgs e)
        {
            if (SprNastav_cBox.SelectedIndex == -1)
            {
                return;
            }

            Sprejemniki1 brisan = Sprejemniki1.FirstOrDefault(p => p.Ime == SprNastav_cBox.Text);
            BISS brisanB = BISS_data.FirstOrDefault(p => p.Sprejemnik == SprNastav_cBox.Text);
            
            DialogResult dialogResult = MessageBox.Show("Zbriši "+ brisan.Ime + " iz baze?", "POZOR!", MessageBoxButtons.YesNo);

            if (dialogResult == DialogResult.Yes)
            {
                Sprejemniki1.Remove(brisan);
                BISS_data.Remove(brisanB);

                //POSODOBI BAZO SPREJEMNIKOV IN COMBOBOXE
                using (TextWriter tw = new StreamWriter(bp.Sprejemniki_CSP_Baza_path))
                {
                    Sprejemniki.Items.Clear();
                    SprNastav_cBox.Items.Clear();

                    foreach (Sprejemniki1 x in Sprejemniki1.OrderBy(c => c.Ime, new NaturalSortComparer<string>()))
                    {
                        tw.WriteLine(x.Ime + "," + x.Proizvajalec + "," + x.IP + "," + x.SwitchPort1 + "," + x.Switchport2
                             + "," + x.Tip + "," + x.LMBPort + "," + x.SigNoiseOID + "," + x.LockedOID + "," + x.Switch);

                        Sprejemniki.Items.Add(x.Ime);
                        SprNastav_cBox.Items.Add(x.Ime);
                    }
                }

                using (TextWriter tw = new StreamWriter(bp.BISS_CSP_Baza_path))
                {
                    foreach (BISS y in BISS_data)
                    {
                        tw.WriteLine(y.Sprejemnik + "," + y.Koda);
                    }
                }

                SprNastav_cBox.SelectedItem = -1;
                SprNastav_cBox.Text = "IZBERI SPREJEMNIK";
            }
        }
        
        //ZAPIŠI SPREMEMBE V BAZO
        public void PosodobiBazo(string BazaPath, List<string> TempList)
        {
            using (TextWriter tw = new StreamWriter(BazaPath))
            {
                foreach (String s in TempList)
                    tw.WriteLine(s);
            }

            TempList.Clear();
        }

        //Posodobitev ComboBoxa
        public void Posodobi_ComboBox(string BazaPath, ComboBox TargetBox)
        {
            //Posodobi Dropdown Menije
            string imeNaprave;
            int steviloNaprav = Convert.ToInt32(File.ReadLines(BazaPath).Skip(0).Take(1).First());
            int perioda = Convert.ToInt32(File.ReadLines(BazaPath).Skip(1).Take(1).First());

            TargetBox.Items.Clear();

            for (int i = 0; i < steviloNaprav; i++)
            {
                imeNaprave = File.ReadLines(BazaPath).Skip(perioda * i + 2).Take(1).First();
                TargetBox.Items.Add(imeNaprave);
            }
        }

        //Preverjanje pravilnosti IP naslova
        public bool ValidateIPv4(string ipString)
        {
            if (String.IsNullOrWhiteSpace(ipString))
            {
                return false;
            }

            string[] splitValues = ipString.Split('.');
            if (splitValues.Length != 4)
            {
                return false;
            }

            byte tempForParsing;

            return splitValues.All(r => byte.TryParse(r, out tempForParsing));
        }


        //NASTAVITVE ENC-PC -----------
        //-----------------------------
        //-----------------------------
        //-----------------------------

        //COMBOBOX SELECTION
        private void ENC_Nastav_cBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(ENC_Nastav_cBox.SelectedIndex == -1)
            {
                return;
            }

            Enkoderji NastavEncCBox = ENC_data.FirstOrDefault(p => p.Ime == ENC_Nastav_cBox.Text);
            
            Nastav_ENC_IME_label.Text = NastavEncCBox.Ime;
            Nastav_ENC_IP_label.Text = NastavEncCBox.IP;
            Nastav_ENC_Switch_label.Text = NastavEncCBox.Switch;
            Nastav_ENC_Port_label.Text = NastavEncCBox.Switchport;
            Nastav_ENC_Brisanje_button.Visible = true;
        }

        //PREKLOP DODAJ-UREDI
        private void Nastav_ENC_dodaj_Uredi_button_Click(object sender, EventArgs e)
        {
            if (Nastav_ENC_dodaj_Uredi_button.Text == "DODAJANJE ENC/PC")
            {
                ENC_Nastav_cBox.Enabled = false;
                Nastav_ENC_IME_label.Text = "Novo Ime";
                Nastav_ENC_IP_label.Text = "Nov IP";
                Nastav_ENC_Switch_comboBox.SelectedIndex = -1;
                Nastav_ENC_Port_label.Text = "Nov Port";
                Nastav_ENC_dodaj_Uredi_button.Text = "UREJANJE ENC/PC";
                Nastav_ENC_Brisanje_button.Visible = false;
                Nastav_ENC_Urejanje_Button.Text = "DODAJ";
                return;
            }

            if (Nastav_ENC_dodaj_Uredi_button.Text == "UREJANJE ENC/PC")
            {
                ENC_Nastav_cBox.Enabled = true;
                ENC_Nastav_cBox.SelectedIndex = -1;
                ENC_Nastav_cBox.Text = "IZBERI ENC/PC";
                Nastav_ENC_IME_label.Text = "";
                Nastav_ENC_IP_label.Text = "";
                Nastav_ENC_Switch_comboBox.SelectedIndex = -1;
                Nastav_ENC_Port_label.Text = "";
                Nastav_ENC_dodaj_Uredi_button.Text = "DODAJANJE ENC/PC";
                Nastav_ENC_Brisanje_button.Visible = true;
                Nastav_ENC_Urejanje_Button.Text = "UREDI";
            }
        }

        //BRISANJE ENC / PC
        private void Nastav_ENC_Brisanje_button_Click(object sender, EventArgs e)
        {

            Enkoderji BrisEnc = ENC_data.FirstOrDefault(p => p.Ime == ENC_Nastav_cBox.Text);

            if (ENC_Nastav_cBox.SelectedIndex == -1)
            {
                return;
            }

            int k = ENC_Nastav_cBox.SelectedIndex;

            DialogResult dialogResult = MessageBox.Show("Zbriši " + Nastav_ENC_IME_label.Text + " iz baze?", "POZOR!", MessageBoxButtons.YesNo);

            if (dialogResult == DialogResult.Yes)
            {
                ENC_data.Remove(BrisEnc);

                //POSODOBI BAZO Enc IN COMBOBOXE
                using (TextWriter tw = new StreamWriter(bp.ENC_CSP_Baza_path))
                {
                    IzberiENC_PCcomboBox.Items.Clear();
                    ENC_Nastav_cBox.Items.Clear();

                    foreach (Enkoderji x in ENC_data.OrderBy(c => c.Ime, new NaturalSortComparer<string>()))
                    {
                        tw.WriteLine(x.Ime + "," + x.IP + "," + x.Switchport + "," + x.Switch);

                        IzberiENC_PCcomboBox.Items.Add(x.Ime);
                        ENC_Nastav_cBox.Items.Add(x.Ime);
                    }
                }
                ENC_Nastav_cBox.SelectedItem = -1;
                ENC_Nastav_cBox.Text = "IZBERI ENC/PC";
            }
        }
        
        //UREJANJE IN DODAJANJE ENC / PC
        private void Nastav_ENC_Urejanje_Button_Click(object sender, EventArgs e)
        {
            if (ENC_Nastav_cBox.SelectedIndex != -1 && Nastav_ENC_Urejanje_Button.Text == "UREDI")
            {
                Enkoderji EncUrejan = ENC_data.FirstOrDefault(p => p.Ime == ENC_Nastav_cBox.Text);

                int k = ENC_Nastav_cBox.SelectedIndex;

                if (Nastav_ENC_IME_textBox.Text != "")
                {
                    EncUrejan.Ime = Nastav_ENC_IME_textBox.Text;
                    Nastav_ENC_IME_label.Text = EncUrejan.Ime;
                    Nastav_ENC_IME_textBox.Text = "";
                }

                if (Nastav_ENC_IP_textBox.Text != "")
                {
                    if (ValidateIPv4(Nastav_ENC_IP_textBox.Text) == true)
                    {
                        EncUrejan.IP = Nastav_ENC_IP_textBox.Text;
                        Nastav_ENC_IP_label.Text = EncUrejan.IP;
                    }
                    else
                    {
                        EncUrejan.IP = "0.0.0.0";
                        Nastav_ENC_IP_label.Text = EncUrejan.IP;
                    }
                    Nastav_ENC_IP_textBox.Text = "";
                }

                if(Nastav_ENC_Switch_comboBox.SelectedIndex != -1)
                {
                    EncUrejan.Switch = Nastav_ENC_Switch_comboBox.Text;
                }
                Nastav_ENC_Switch_label.Text = EncUrejan.Switch;

                if (Nastav_ENC_Port_textBox.Text != "")
                {
                    if (Nastav_ENC_Port_textBox.Text.Length == 1 && Nastav_ENC_Port_textBox.Text != "0")
                    {
                        EncUrejan.Switchport = "0" + Nastav_ENC_Port_textBox.Text;
                    }
                    else
                    {
                        EncUrejan.Switchport = Nastav_ENC_Port_textBox.Text;
                    }
                    Nastav_ENC_Port_label.Text = EncUrejan.Switchport;
                    Nastav_ENC_Port_textBox.Text = "";
                }

                //POSODOBI BAZO Enc IN COMBOBOXE
                using (TextWriter tw = new StreamWriter(bp.ENC_CSP_Baza_path))
                {
                    IzberiENC_PCcomboBox.Items.Clear();
                    ENC_Nastav_cBox.Items.Clear();

                    foreach (Enkoderji x in ENC_data.OrderBy(c => c.Ime, new NaturalSortComparer<string>()))
                    {
                        tw.WriteLine(x.Ime + "," + x.IP + "," + x.Switchport + "," + x.Switch);

                        IzberiENC_PCcomboBox.Items.Add(x.Ime);
                        ENC_Nastav_cBox.Items.Add(x.Ime);
                    }
                }
            }

            //DODAJANJE / UREJANJE ENC / PC
            if (Nastav_ENC_Urejanje_Button.Text == "DODAJ")
            {
                if(Nastav_ENC_Switch_comboBox.SelectedIndex == -1)
                {
                    MessageBox.Show("Izbari switch!", "Napaka!)");
                    return;
                }

                if (Nastav_ENC_IME_textBox.Text != "")
                {
                    Enkoderji EncDo = new Enkoderji();

                    EncDo.Ime = Nastav_ENC_IME_textBox.Text; //IME
                    Nastav_ENC_IME_textBox.Text = "";

                    EncDo.Switch = Nastav_ENC_Switch_comboBox.Text;

                    if (Nastav_ENC_Port_textBox.Text != "")
                    {
                        if (Nastav_ENC_Port_textBox.Text.Length == 1 && Nastav_ENC_Port_textBox.Text != "0")
                        {
                            EncDo.Switchport = "0" + Nastav_ENC_Port_textBox.Text;
                        }
                        else
                        {
                            EncDo.Switchport = Nastav_ENC_Port_textBox.Text;
                        }
                        Nastav_ENC_Port_label.Text = EncDo.Switchport;
                        Nastav_ENC_Port_textBox.Text = "";
                    }
                    
                    if (ValidateIPv4(Nastav_ENC_IP_textBox.Text) == true)
                    {
                        EncDo.IP = Nastav_ENC_IP_textBox.Text; //IP naslov
                        Nastav_ENC_IP_textBox.Text = "";
                    }
                    else
                    {
                        EncDo.IP = "0.0.0.0";
                        Nastav_ENC_IP_textBox.Text = "";
                    }

                    ENC_data.Add(EncDo);

                    //POSODOBI BAZO Enc IN COMBOBOXE
                    using (TextWriter tw = new StreamWriter(bp.ENC_CSP_Baza_path))
                    {
                        IzberiENC_PCcomboBox.Items.Clear();
                        ENC_Nastav_cBox.Items.Clear();

                        foreach (Enkoderji x in ENC_data.OrderBy(c => c.Ime, new NaturalSortComparer<string>()))
                        {
                            tw.WriteLine(x.Ime + "," + x.IP + "," + x.Switchport + "," + x.Switch);

                            IzberiENC_PCcomboBox.Items.Add(x.Ime);
                            ENC_Nastav_cBox.Items.Add(x.Ime);
                        }
                    }

                    ENC_Nastav_cBox.SelectedItem = Nastav_ENC_IME_label.Text;

                    MessageBox.Show("ENC/PC uspešno dodan!", "JUHEJ!");
                }
            }
        }


        //NASTAVITVE MULTICAST PRESETOV
        //-----------------------------
        //-----------------------------
        //-----------------------------

        //IZBIRA MC PRESETA
        private void Multi_Nastav_cBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (Multi_Nastav_cBox.SelectedIndex == -1)
            {
                return;
            }

            IzbranMC = Multi_data.FirstOrDefault(p => p.Ime == Multi_Nastav_cBox.Text);
            
            Nastav_Multi_Ime_label.Text = IzbranMC.Ime;
            Nastav_Multi_IP_label.Text = IzbranMC.IP;
            Nastav_Multi_UDP_label.Text = IzbranMC.UDP;
            Nastav_Multi_VLAN_label.Text = IzbranMC.VLAN;

            Nastav_Multi_Briši_button.Visible = true;
        }

        //BRISANJE MULTICAST PRESETOV
        private void Nastav_Multi_Briši_button_Click(object sender, EventArgs e)
        {
            if (Multi_Nastav_cBox.SelectedIndex == -1)
            {
                return;
            }
            DialogResult dialogResult = MessageBox.Show("Zbriši " + Nastav_Multi_Ime_label.Text + " iz baze?", "POZOR!", MessageBoxButtons.YesNo);

            if (dialogResult == DialogResult.Yes)
            {
                Multi_data.Remove(IzbranMC);

                //POSODOBI BAZO Multi IN COMBOBOXE
                using (TextWriter tw = new StreamWriter(bp.Multi_CSP_Baza_path))
                {
                    MultiPresetsComboBox.Items.Clear();
                    Multi_Nastav_cBox.Items.Clear();

                    foreach (Multicasts x in Multi_data.OrderBy(c => c.Ime, new NaturalSortComparer<string>()))
                    {
                        tw.WriteLine(x.Ime + "," + x.IP + "," + x.UDP + "," + x.VLAN);

                        MultiPresetsComboBox.Items.Add(x.Ime);
                        Multi_Nastav_cBox.Items.Add(x.Ime);
                    }
                }
                Multi_Nastav_cBox.SelectedItem = -1;
                Multi_Nastav_cBox.Text = "IZBERI MULTICAST";
            }
        }

        //PREKLOP UREJANJE-DODAJANJE MULTICASTA
        private void Nastav_Multi_Dodaj_Uredi_button_Click(object sender, EventArgs e)
        {
            if (Nastav_Multi_Dodaj_Uredi_button.Text == "DODAJANJE MULTICASTA")
            {
                Multi_Nastav_cBox.Enabled = false;
                Nastav_Multi_Ime_label.Text = "Novo Ime";
                Nastav_Multi_IP_label.Text = "Nov Multi IP";
                Nastav_Multi_UDP_label.Text = "Nov UDP Port";
                Nastav_Multi_VLAN_label.Text = "Nov VLAN";
                Nastav_Multi_Dodaj_Uredi_button.Text = "UREJANJE MULTICASTA";
                Nastav_Multi_Briši_button.Visible = false;
                Nastav_Multi_Uredi_button.Text = "DODAJ";
                return;
            }

            if (Nastav_Multi_Dodaj_Uredi_button.Text == "UREJANJE MULTICASTA")
            {
                Multi_Nastav_cBox.Enabled = true;
                Multi_Nastav_cBox.SelectedIndex = -1;
                Multi_Nastav_cBox.Text = "IZBERI MULTICAST";
                Nastav_Multi_Ime_label.Text = "";
                Nastav_Multi_IP_label.Text = "";
                Nastav_Multi_UDP_label.Text = "";
                Nastav_Multi_VLAN_label.Text = "";
                Nastav_Multi_Dodaj_Uredi_button.Text = "DODAJANJE MULTICASTA";
                Nastav_Multi_Briši_button.Visible = true;
                Nastav_Multi_Uredi_button.Text = "UREDI";
            }
        }

        //DODAJANJE-UREJANJE MULTICASTA
        private void Nastav_Multi_Uredi_button_Click(object sender, EventArgs e)
        {
            //urejanje multicasta
            if (Multi_Nastav_cBox.SelectedIndex != -1 && Nastav_Multi_Uredi_button.Text == "UREDI")
            {
                IzbranMC = Multi_data.FirstOrDefault(p => p.Ime == Multi_Nastav_cBox.Text);

                int k = Multi_Nastav_cBox.SelectedIndex;

                if (Nastav_Multi_Ime_textBox.Text != "")
                {
                    IzbranMC.Ime = Nastav_Multi_Ime_textBox.Text;
                    Nastav_Multi_Ime_label.Text = IzbranMC.Ime;
                    Nastav_Multi_Ime_textBox.Text = "";
                }

                if (Nastav_Multi_IP_textBox.Text != "")
                {
                    if (ValidateIPv4(Nastav_Multi_IP_textBox.Text) == true)
                    {
                        IzbranMC.IP = Nastav_Multi_IP_textBox.Text;
                        Nastav_Multi_IP_label.Text = IzbranMC.IP;
                    }
                    else
                    {
                        IzbranMC.IP = "0.0.0.0";
                        Nastav_Multi_IP_label.Text = IzbranMC.IP;
                    }
                    Nastav_Multi_IP_textBox.Text = "";
                }

                if (Nastav_Multi_UDP_textBox.Text != "")
                {
                    try
                    {
                        Convert.ToInt32(Nastav_Multi_UDP_textBox.Text);
                        IzbranMC.UDP = Nastav_Multi_UDP_textBox.Text;
                    }
                    catch
                    {
                        IzbranMC.UDP = "0";
                    }
                    Nastav_ENC_Port_label.Text = IzbranMC.UDP;
                    Nastav_Multi_UDP_textBox.Text = "";
                }

                if (Nastav_Multi_VLAN_textBox.Text != "")
                {
                    try
                    {
                        Convert.ToInt32(Nastav_Multi_VLAN_textBox.Text);
                        IzbranMC.VLAN = Nastav_Multi_VLAN_textBox.Text;
                    }

                    catch
                    {
                        IzbranMC.VLAN = "0";
                    }
                    Nastav_ENC_Port_label.Text = IzbranMC.VLAN;
                    Nastav_Multi_VLAN_textBox.Text = "";
                }

                //POSODOBI BAZO Multi IN COMBOBOXE
                using (TextWriter tw = new StreamWriter(bp.Multi_CSP_Baza_path))
                {
                    MultiPresetsComboBox.Items.Clear();
                    Multi_Nastav_cBox.Items.Clear();

                    foreach (Multicasts x in Multi_data.OrderBy(c => c.Ime, new NaturalSortComparer<string>()))
                    {
                        tw.WriteLine(x.Ime + "," + x.IP + "," + x.UDP + "," + x.VLAN);

                        MultiPresetsComboBox.Items.Add(x.Ime);
                        Multi_Nastav_cBox.Items.Add(x.Ime);
                    }
                }
                
                Multi_Nastav_cBox.SelectedItem = Nastav_Multi_Ime_label.Text;
            }

            //DODAJANJE multicasta
            if (Nastav_Multi_Uredi_button.Text == "DODAJ")
            {
                
                if (Nastav_Multi_Ime_textBox.Text != "")
                {

                    Multicasts NovMC = new Multicasts();
                    
                    NovMC.Ime = Nastav_Multi_Ime_textBox.Text; //IME
                    Nastav_Multi_Ime_textBox.Text = "";

                    if (ValidateIPv4(Nastav_Multi_IP_textBox.Text) == true)
                    {
                        NovMC.IP = Nastav_Multi_IP_textBox.Text; //IP naslov
                        Nastav_Multi_IP_textBox.Text = "";
                    }
                    else
                    {
                        NovMC.IP = "0.0.0.0";
                        Nastav_Multi_IP_textBox.Text = "";
                    }

                    try
                    {
                        Convert.ToInt32(Nastav_Multi_UDP_textBox.Text);
                        NovMC.UDP = Nastav_Multi_UDP_textBox.Text; //UDP
                    }
                    catch
                    {
                        NovMC.UDP = "0"; //UDP
                    }
                    
                    Nastav_Multi_UDP_textBox.Text = "";

                    try
                    {
                        Convert.ToInt32(Nastav_Multi_VLAN_textBox.Text);
                        NovMC.VLAN = Nastav_Multi_VLAN_textBox.Text; //VLAN
                    }
                    catch
                    {
                        NovMC.VLAN = "0"; //VLAN
                    }
                        
                    Nastav_Multi_VLAN_textBox.Text = "";

                    Multi_data.Add(NovMC);

                    //POSODOBI BAZO Multi IN COMBOBOXE
                    using (TextWriter tw = new StreamWriter(bp.Multi_CSP_Baza_path))
                    {
                        MultiPresetsComboBox.Items.Clear();
                        Multi_Nastav_cBox.Items.Clear();

                        foreach (Multicasts x in Multi_data.OrderBy(c => c.Ime, new NaturalSortComparer<string>()))
                        {
                            tw.WriteLine(x.Ime + "," + x.IP + "," + x.UDP + "," + x.VLAN);

                            MultiPresetsComboBox.Items.Add(x.Ime);
                            Multi_Nastav_cBox.Items.Add(x.Ime);
                        }
                    }
                    
                    MessageBox.Show("Multicast uspešno dodan!", "JUHEJ!");
                }
                
            }
        }
        
        private void IzbiraKanala(object sender, EventArgs e)
        {
            try
            {
                SatKanali c = SatKanali.FirstOrDefault(p => p.Satelit == Satelit_comboBox.Text && p.Modulacija == Modulacija_comboBox.Text && p.Kanal == kanal_textbox.Text.ToUpper());

                //spremeni pike v vejice da dela
                try
                {
                    string[] mfreq = c.frekvence.Split('.');
                    FrekvencaTextBox.Text = mfreq[0] + "," + mfreq[1];
                }
                catch
                {
                    FrekvencaTextBox.Text = c.frekvence;
                }

                try
                {
                    string[] nSR = c.SR.Split('.');
                    SymRateTextBox.Text = nSR[0] + "," + nSR[1];
                }
                catch
                {
                    SymRateTextBox.Text = c.SR;
                }
                
                Polartizacija_textBox.Text = c.polarizacija;
            }

            catch
            {
                FrekvencaTextBox.Text = "";
                SymRateTextBox.Text = "";
                Polartizacija_textBox.Text = "";
            }
        }

        //Sidebar Refresh
        private async void SidebarTimer_Tick(object sender, EventArgs e)
        {
            foreach(Sprejemniki1 c in Sprejemniki1)
            {
                Label myControl;

                try
                {
                    myControl = Controls.Find(c.Ime, true).FirstOrDefault() as Label;
                }
                catch
                {
                    continue;
                }
                

                if(ping(c.IP) == false)
                {
                    myControl.Text = c.Ime + "  ";
                    myControl.BackColor = Color.WhiteSmoke;
                    continue;
                }
                
                if (c.Proizvajalec == "ateme")
                {
                    try
                    {
                        myControl.Text = c.Ime + "  " + Convert.ToString(Convert.ToDecimal(SNMPGet(c.IP, c.SigNoiseOID)) / 10) + " dB";
                    }
                    catch
                    {
                        myControl.Text = c.Ime + "  ";
                    }

                    if (SNMPGet(c.IP, c.LockedOID) == "2")
                    {
                        myControl.BackColor = Color.WhiteSmoke;
                    }
                    else if (SNMPGet(c.IP, c.LockedOID) == "1")
                    {
                        myControl.BackColor = Color.LightSeaGreen;
                    }
                }

                else if(c.Tip == "IP ONLY")
                {
                    if (SNMPGet(c.IP, c.LockedOID) == "0")
                    {
                        myControl.BackColor = Color.WhiteSmoke;
                    }
                    else if (SNMPGet(c.IP, c.LockedOID) == "1")
                    {
                        myControl.BackColor = Color.LightSeaGreen;
                    }
                }

                else
                {
                    myControl.Text = c.Ime + "  " + SNMPGet(c.IP, c.SigNoiseOID);

                    if (SNMPGet(c.IP, c.LockedOID) == "0")
                    {
                        myControl.BackColor = Color.WhiteSmoke;
                    }
                    else if (SNMPGet(c.IP, c.LockedOID) == "1")
                    {
                        myControl.BackColor = Color.LightSeaGreen;
                    }
                }
            }
        }

        //SNMP GET ZAHTEVEK
        public string SNMPGet(string TargetIPAdress, string OID)
        {
            if (OID == "0")
            {
                return "";
            }
            try
            {
                OctetString community = new OctetString("private");
                AgentParameters param = new AgentParameters(community);
                param.Version = SnmpVersion.Ver2;
                IpAddress agent = new IpAddress(TargetIPAdress);
                if (!agent.Valid)
                {
                    return "Napačen IP naslov!";
                }
                UdpTarget target = new UdpTarget((IPAddress)agent, 161, 1000, 3); //Parametri za timeout
                Pdu pdu = new Pdu(PduType.Get);
                pdu.VbList.Add(OID);
                try
                {
                    SnmpV2Packet result = (SnmpV2Packet)target.Request(pdu, param);
                    if (result != null)
                    {
                        if (result.Pdu.ErrorStatus != 0)
                        {
                            return ("Error in SNMP reply.");
                        }

                        else if (result.Pdu.VbList[0].Value.ToString() == "SNMP No-Such-Object")
                        {
                            return ("NAPAKA");
                        }

                        else
                        {
                            return (result.Pdu.VbList[0].Value.ToString());
                        }
                    }
                    else
                    {
                        return ("No response received from SNMP agent.");
                    }
                }

                catch
                {
                    return ("Napaka pri pošiljanju snmp paketa!");
                }
            }
            catch
            {
                return "";
            }
        }

        //NASTAVITVE SWITCHOV
        //-------------------
        //-------------------
        //-------------------

        //IZBIRA SWITCHA
        private void Nastav_Switchi_cBox_SelectedChange(object sender, EventArgs e)
        {
            if(Nastav_Switchi_cBox.SelectedIndex == -1)
            {
                return;
            }

            Izbranswitch = Switch_data.FirstOrDefault(p => p.Ime == Nastav_Switchi_cBox.Text);

            Nastav_Switchi_IME_label.Text = Izbranswitch.Ime;
            Nastav_Switchi_IP_label.Text = Izbranswitch.IP;
            Nastav_Switchi_Brisanje_button.Visible = true;
        }

        private void Nastav_Switchi_Dodaj_Uredi_button_Click(object sender, EventArgs e)
        {
            if(Nastav_Switchi_Dodaj_Uredi_button.Text == "DODAJANJE SWITCHEV")
            {
                Nastav_Switchi_Dodaj_Uredi_button.Text = "UREJANJE SWITCHEV";
                Nastav_Switchi_cBox.SelectedIndex = -1;
                Nastav_Switchi_cBox.Enabled = false;
                Nastav_Switchi_Brisanje_button.Visible = false;
                Nastav_Switchi_IME_label.Text = "";
                Nastav_Switchi_IP_label.Text = "";
                Nastav_Switchi_Uredi_button.Text = "DODAJ";
                return;
            }

            if (Nastav_Switchi_Dodaj_Uredi_button.Text == "UREJANJE SWITCHEV")
            {
                Nastav_Switchi_Dodaj_Uredi_button.Text = "DODAJANJE SWITCHEV";
                Nastav_Switchi_cBox.Enabled = true;
                Nastav_Switchi_IME_label.Text = "";
                Nastav_Switchi_IP_label.Text = "";
                Nastav_Switchi_Uredi_button.Text = "UREDI";
            }
        }

        //brisanje switcha iz baze
        private void Nastav_Switchi_Brisanje_button_Click(object sender, EventArgs e)
        {
            if (Nastav_Switchi_cBox.SelectedIndex == -1)
            {
                return;
            }
            
            DialogResult dialogResult = MessageBox.Show("Zbriši " + Nastav_Switchi_IME_label.Text + " iz baze?", "POZOR!", MessageBoxButtons.YesNo);

            if (dialogResult == DialogResult.Yes)
            {
                Switch_data.Remove(Izbranswitch);

                //POSODOBI BAZO switcha IN COMBOBOXE
                using (TextWriter tw = new StreamWriter(bp.Switch_CSP_Baza_path))
                {
                    // Manjka en kup comboboxov ki še ne obstajajo - enc, dec etc...
                    Nastav_Switchi_cBox.Items.Clear();
                    Nastav_SPR_Switch_comboBox.Items.Clear();
                    Nastav_ENC_Switch_comboBox.Items.Clear();

                    foreach (SwitchData x in Switch_data.OrderBy(c => c.Ime, new NaturalSortComparer<string>()))
                    {
                        tw.WriteLine(x.Ime + "," + x.IP);

                        // Manjka en kup comboboxov ki še ne obstajajo - enc, dec etc...
                        Nastav_Switchi_cBox.Items.Add(x.Ime);
                        Nastav_SPR_Switch_comboBox.Items.Add(x.Ime);
                        Nastav_ENC_Switch_comboBox.Items.Add(x.Ime);
                    }
                }
                Nastav_Switchi_cBox.SelectedItem = -1;
                Nastav_Switchi_cBox.Text = "IZBERI SWITCH";
            }
        }

        //DODAJANJE IN UREJANJE SWITCHOV
        private void Nastav_Switchi_Uredi_button_Click(object sender, EventArgs e)
        {
            if (Nastav_Switchi_cBox.SelectedIndex != -1 && Nastav_Switchi_Uredi_button.Text == "UREDI")
            {
                Izbranswitch = Switch_data.FirstOrDefault(p => p.Ime == Nastav_Switchi_cBox.Text);
                
                if (Nastav_Switchi_IME_textBox.Text != "")
                {
                    Izbranswitch.Ime = Nastav_Switchi_IME_textBox.Text;
                    Nastav_Switchi_IME_label.Text = Izbranswitch.Ime;
                    Nastav_Switchi_IME_textBox.Text = "";
                }

                if (Nastav_Switchi_IP_textBox.Text != "")
                {
                    if (ValidateIPv4(Nastav_Switchi_IP_textBox.Text) == true)
                    {
                        Izbranswitch.IP = Nastav_Switchi_IP_textBox.Text;
                        Nastav_Switchi_IP_label.Text = Izbranswitch.IP;
                    }
                    else
                    {
                        Izbranswitch.IP = "0.0.0.0";
                        Nastav_Switchi_IP_label.Text = Izbranswitch.IP;
                    }
                    Nastav_Switchi_IP_textBox.Text = "";
                }
                
                //POSODOBI BAZO switcha IN COMBOBOXE
                using (TextWriter tw = new StreamWriter(bp.Switch_CSP_Baza_path))
                {
                    
                    Nastav_Switchi_cBox.Items.Clear();
                    Nastav_SPR_Switch_comboBox.Items.Clear();
                    Nastav_ENC_Switch_comboBox.Items.Clear();

                    foreach (SwitchData x in Switch_data.OrderBy(c => c.Ime, new NaturalSortComparer<string>()))
                    {
                        tw.WriteLine(x.Ime + "," + x.IP);
                        
                        Nastav_Switchi_cBox.Items.Add(x.Ime);
                        Nastav_SPR_Switch_comboBox.Items.Add(x.Ime);
                        Nastav_ENC_Switch_comboBox.Items.Add(x.Ime);
                    }
                }
                Nastav_Switchi_cBox.SelectedItem = Nastav_Switchi_IME_label.Text;
            }

            //DODAJANJE SWITCHA
            if (Nastav_Switchi_Uredi_button.Text == "DODAJ")
            {

                if (Nastav_Switchi_IME_textBox.Text != "")
                {

                    SwitchData NovSW = new SwitchData();

                    NovSW.Ime = Nastav_Switchi_IME_textBox.Text; //IME
                    Nastav_Switchi_IME_textBox.Text = "";

                    if (ValidateIPv4(Nastav_Switchi_IP_textBox.Text) == true)
                    {
                        NovSW.IP = Nastav_Switchi_IP_textBox.Text; //IP naslov
                        Nastav_Switchi_IP_textBox.Text = "";
                    }
                    else
                    {
                        NovSW.IP = "0.0.0.0";
                        Nastav_Switchi_IP_textBox.Text = "";
                    }
                    
                    Switch_data.Add(NovSW);

                    //POSODOBI BAZO Multi IN COMBOBOXE
                    using (TextWriter tw = new StreamWriter(bp.Switch_CSP_Baza_path))
                    {
                        Nastav_Switchi_cBox.Items.Clear();
                        Nastav_SPR_Switch_comboBox.Items.Clear();
                        Nastav_ENC_Switch_comboBox.Items.Clear();

                        foreach (SwitchData x in Switch_data.OrderBy(c => c.Ime, new NaturalSortComparer<string>()))
                        {
                            tw.WriteLine(x.Ime + "," + x.IP);

                            Nastav_Switchi_cBox.Items.Add(x.Ime);
                            Nastav_SPR_Switch_comboBox.Items.Add(x.Ime);
                            Nastav_ENC_Switch_comboBox.Items.Add(x.Ime);
                        }
                    }
                    MessageBox.Show("Switch uspešno dodan!", "JUHEJ!");
                }
            }
        }

        private void Ostale_Vmesnik_button_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://" + IzbranENC.IP);
        }
    }

    //Class za podatke kanalov
    public class SatKanali
    {
        public string Satelit { get; set; }
        public string Modulacija { get; set; }
        public string Kanal { get; set; }
        public string frekvence { get; set; }
        public string SR { get; set; }
        public string polarizacija { get; set; }
    }

    //Class za podatke Sprejemnikov
    public class Sprejemniki1
    {
        public string Ime { get; set; }
        public string Proizvajalec { get; set; }
        public string IP { get; set; }
        public string SwitchPort1 { get; set; }
        public string Switchport2 { get; set; }
        public string Tip { get; set; }
        public string LMBPort { get; set; }
        public string SigNoiseOID { get; set; }
        public string LockedOID { get; set; }
        public string Switch { get; set; }
    }

    //Class za podatke LBM
    public class LBM
    {
        public string Ime { get; set; }
        public string InputPort { get; set; }
    }

    //Class za podatke BISS
    public class BISS
    {
        public string Sprejemnik { get; set; }
        public string Koda { get; set; }
    }

    //Class za OID 
    public class OID
    {
        public string Ime { get; set; }
        public string SigNoiseOID { get; set; }
        public string LockedOID { get; set; }
    }

    public class Enkoderji
    {
        public string Ime { get; set; }
        public string Switchport { get; set; }
        public string IP { get; set; }
        public string Switch { get; set; }
    }

    public class VLANs
    {
        public string Ime { get; set; }
        public string Stevilo { get; set; }
    }

    public class Multicasts
    {
        public string Ime { get; set; }
        public string IP { get; set; }
        public string UDP { get; set; }
        public string VLAN { get; set; }
    }

    public class SwitchData
    {
        public string Ime { get; set; }
        public string IP { get; set; }
    }

    //Poti do datotek
    public class BasePaths
    {
        public string ENC_CSP_Baza_path { get; set; } = @"C:\Users\Public\TVRNADZORdata\CSP\ENKODERJI_CSP.txt";
        public string Multi_CSP_Baza_path { get; set; } = @"C:\Users\Public\TVRNADZORdata\CSP\multi_presets_CSP.txt";
        public string BISS_CSP_Baza_path { get; set; } = @"C:\Users\Public\TVRNADZORdata\CSP\BISS_CSP.txt";
        public string LBM_CSP_Baza_path { get; set; } = @"C:\Users\Public\TVRNADZORdata\CSP\LBM_INPUT_Baza_CSP.txt";
        public string Kanali_CSP_Baza_path { get; set; } = @"C:\Users\Public\TVRNADZORdata\CSP\rx_channels_CSP.txt";
        public string Sprejemniki_CSP_Baza_path { get; set; } = @"C:\Users\Public\TVRNADZORdata\CSP\sprejemniki_CSP.txt";
        public string VLAN_CSP_Baza_path { get; set; } = @"C:\Users\Public\TVRNADZORdata\CSP\VLANi_CSP.txt";
        public string OID_CSP_Baza_path { get; set; } = @"C:\Users\Public\TVRNADZORdata\CSP\OID_CSP.txt";
        public string Switch_CSP_Baza_path { get; set; } = @"C:\Users\Public\TVRNADZORdata\CSP\switchi_csp.txt";
    }


    /// <summary>
    /// OD TU NAPREJ JE UKRADENA KODA, SICER PREPOVEDANA Z ŽENEVSKO KONVENCIJO, AMPAK POSKRBI ZA TO DA PALČKI DELAJO 
    /// TO KAR JE TREBA. VERJETNO BI BILA LAHKO SKRITA V KAKEM DRUGEM FAJLU.
    /// </summary>

    //Prepreči utirpanje ozadja. Bad monitor palčki!
    public static class Extensions
    {
        public static void EnableDoubleBuferring(this Control control)
        {
            var property = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            property.SetValue(control, true, null);
        }
    }

    //alphanumeric compare za zapisovanje v baze po vrsti etc....to ne vem če še potrebujem; note to self: vprašaj palčke!
    public class NaturalSortComparer<T> : IComparer<string>, IDisposable
    {
        private bool isAscending;

        public NaturalSortComparer(bool inAscendingOrder = true)
        {
            this.isAscending = inAscendingOrder;
        }

        #region IComparer<string> Members

        public int Compare(string x, string y)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IComparer<string> Members

        int IComparer<string>.Compare(string x, string y)
        {
            if (x == y)
                return 0;

            string[] x1, y1;

            if (!table.TryGetValue(x, out x1))
            {
                x1 = Regex.Split(x.Replace(" ", ""), "([0-9]+)");
                table.Add(x, x1);
            }

            if (!table.TryGetValue(y, out y1))
            {
                y1 = Regex.Split(y.Replace(" ", ""), "([0-9]+)");
                table.Add(y, y1);
            }

            int returnVal;

            for (int i = 0; i < x1.Length && i < y1.Length; i++)
            {
                if (x1[i] != y1[i])
                {
                    returnVal = PartCompare(x1[i], y1[i]);
                    return isAscending ? returnVal : -returnVal;
                }
            }

            if (y1.Length > x1.Length)
            {
                returnVal = 1;
            }
            else if (x1.Length > y1.Length)
            {
                returnVal = -1;
            }
            else
            {
                returnVal = 0;
            }

            return isAscending ? returnVal : -returnVal;
        }

        private static int PartCompare(string left, string right)
        {
            int x, y;
            if (!int.TryParse(left, out x))
                return left.CompareTo(right);

            if (!int.TryParse(right, out y))
                return left.CompareTo(right);

            return x.CompareTo(y);
        }

        #endregion

        private Dictionary<string, string[]> table = new Dictionary<string, string[]>();

        public void Dispose()
        {
            table.Clear();
            table = null;
        }
    }
}