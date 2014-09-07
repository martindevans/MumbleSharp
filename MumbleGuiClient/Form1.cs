using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MumbleSharp;

namespace MumbleGuiClient
{
    public partial class Form1 : Form
    {
        MumbleConnection connection;
        EventBasedProtocol protocol;
        public Form1()
        {
            InitializeComponent();

            connection = new MumbleConnection("", 64738); //TODO: Add your hostname or add some GUI for that
            protocol = connection.Connect<EventBasedProtocol>("testuser", "", "");
            protocol.MessageRecieved += Protocol_MessageRecieved;
            while (connection.Protocol.LocalUser == null)
            {
                connection.Process();
            }

            //MessageBox.Show("Connected as " + connection.Protocol.LocalUser.Id);
        }

        private void Protocol_MessageRecieved(object sender, Message e)
        {
            tbLog.Text += string.Format("{0}({1}): {2}\n", e.Sender, e is ChannelMessage ? ((ChannelMessage)e).Channel.Name : "[PERSONAL]", e.Text);
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            connection.SendTextMessage(tbSendMessage.Text, protocol.LocalUser.Channel);
            tbSendMessage.Text = "";
        }

        private void mumbleUpdater_Tick(object sender, EventArgs e)
        {
            connection.Process();
        }
    }
}
