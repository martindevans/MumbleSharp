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

using Message = MumbleSharp.Model.Message;

namespace MumbleGuiClient
{
    public partial class Form1 : Form
    {
        MumbleConnection connection;
        BasicMumbleProtocol protocol;
        class ChannelTree
        {
            public ChannelTree Parent;
            public Channel Channel;
            public List<ChannelTree> Children = new List<ChannelTree>();
            public List<User> Users = new List<User>();
            public ChannelTree(Channel channel)
            {
                Channel = channel;
            }
        }
        class TreeNode<T> : TreeNode
        {
            public T Value;
        }
        public Form1()
        {
            InitializeComponent();

            connection = new MumbleConnection("mumble.placeholder-software.co.uk", 64738);
            protocol = connection.Connect<BasicMumbleProtocol>("testuser", "", "mumble.placeholder-software.co.uk");
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
            foreach (var user in protocol.Users)
            {
                channels[user.Channel.Id].Users.Add(user);
            }
            ChannelTree RootChannel = channels[0];

            tvUsers.Nodes.Add(MakeNode(RootChannel));
            tvUsers.ExpandAll();

            //MessageBox.Show("Connected as " + connection.Protocol.LocalUser.Id);
        }

        TreeNode MakeNode(ChannelTree tree)
        {
            TreeNode<Channel> result = new TreeNode<Channel>();
            result.Text = tree.Channel.Name;
            result.BackColor = Color.LightBlue; //the colors might be quite ugly
            result.Value = tree.Channel;
            foreach (var child in tree.Children)
            {
                result.Nodes.Add(MakeNode(child));
            }
            foreach (var user in tree.Users)
            {
                TreeNode<User> newNode = new TreeNode<User>();
                newNode.Text = user.Name;
                newNode.BackColor = Color.LightGreen;
                newNode.Value = user;
                result.Nodes.Add(newNode);
            }
            return result;
        }

        private void Protocol_MessageRecieved(object sender, Message e)
        {
            tbLog.Text += string.Format("{0}({1}): {2}\n", e.Sender, e is ChannelMessage ? ((ChannelMessage)e).Channel.Name : "[PERSONAL]", e.Text);
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            Channel target = protocol.LocalUser.Channel;
            if (tvUsers.SelectedNode != null && tvUsers.SelectedNode is TreeNode<Channel>)
            {
                target = ((TreeNode<Channel>)tvUsers.SelectedNode).Value; //This does not seem to work.
            }
            connection.SendTextMessage(tbSendMessage.Text, protocol.LocalUser.Channel);
            tbLog.Text += string.Format("{0} {1}: {2}\n", protocol.LocalUser.Name, target.Name, tbSendMessage.Text); //TODO: Unify, avoid doubled code
            tbSendMessage.Text = "";
        }

        private void mumbleUpdater_Tick(object sender, EventArgs e)
        {
            connection.Process();
        }

        private void tvUsers_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (tvUsers.SelectedNode is TreeNode<Channel>)
            {
                Channel channel = ((TreeNode<Channel>)tvUsers.SelectedNode).Value;
                //Enter that channel, needs the functionality in connection or protocol.
            }
        }
    }
}
