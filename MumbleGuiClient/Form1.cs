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
using MumbleSharp.Model;

namespace MumbleGuiClient
{
    public partial class Form1 : Form
    {
        MumbleConnection connection;
        EventBasedProtocol protocol;
        class ChannelTree
        {
            public ChannelTree Parent;
            public Channel Channel;
            public List<ChannelTree> Children = new List<ChannelTree>();
            public ChannelTree(Channel channel)
            {
                Channel = channel;
            }
        }
        public Form1()
        {
            InitializeComponent();

            connection = new MumbleConnection("mumble.placeholder-software.co.uk", 64738);
            protocol = connection.Connect<EventBasedProtocol>("testuser", "", "mumble.placeholder-software.co.uk");
            protocol.MessageRecieved += Protocol_MessageRecieved;
            while (connection.Protocol.LocalUser == null)
            {
                connection.Process();
            }

            Dictionary<uint, ChannelTree> channels = new Dictionary<uint, ChannelTree>();
            foreach (var channel in protocol.Channels)
            {
                channels.Add(channel.Id, new ChannelTree(channel));
            }
            foreach (var channelTree in channels.Values)
            {
                if (channelTree.Channel.Id != 0)
                {
                    channelTree.Parent = channels[channelTree.Channel.Parent];
                    channelTree.Parent.Children.Add(channelTree);
                }
            }
            ChannelTree RootChannel = channels[0];

            tvUsers.Nodes.Add(MakeNode(RootChannel));

            //MessageBox.Show("Connected as " + connection.Protocol.LocalUser.Id);
        }

        TreeNode MakeNode(ChannelTree tree)
        {
            TreeNode result = new TreeNode(tree.Channel.Name);
            foreach (var child in tree.Children)
            {
                result.Nodes.Add(MakeNode(child));
            }
            return result;
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
