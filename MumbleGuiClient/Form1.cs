using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
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
        ConnectionMumbleProtocol protocol;
        MicrophoneRecorder recorder;
        SpeakerPlayback playback;

        struct ChannelInfo
        {
            public string Name;
            public uint Id;
            public uint Parent;
        }
        struct UserInfo 
        {
            public uint Id;
            public bool Deaf;
            public bool Muted;
            public bool SelfDeaf;
            public bool SelfMuted;
            public bool Supress;
            public uint Channel;
        }

        class TreeNode<T> : TreeNode
        {
            public T Value;
        }
        public Form1()
        {
            InitializeComponent();

            protocol = new ConnectionMumbleProtocol();
            protocol.channelMessageReceivedDelegate = ChannelMessageReceivedDelegate;
            protocol.personalMessageReceivedDelegate = PersonalMessageReceivedDelegate;
            protocol.encodedVoice = EncodedVoiceDelegate;
            protocol.userJoinedDelegate = UserJoinedDelegate;
            protocol.userStateChangedDelegate = UserStateChangedDelegate;
            protocol.userStateChannelChangedDelegate = UserStateChannelChangedDelegate;
            protocol.userLeftDelegate = UserLeftDelegate;
            protocol.channelJoinedDelegate = ChannelJoinedDelegate;
            protocol.channelLeftDelegate = ChannelLeftDelegate;
            protocol.serverConfigDelegate = ServerConfigDelegate;

            tvUsers.ExpandAll();
            tvUsers.StartUpdating();

            playback = new SpeakerPlayback();
            int playbackDeviceCount = NAudio.Wave.WaveOut.DeviceCount;
            cbPlaybackDevices.Items.Add("Default Playback Device");
            for (int i = 0; i < playbackDeviceCount; i++)
            {
                NAudio.Wave.WaveOutCapabilities deviceInfo = NAudio.Wave.WaveOut.GetCapabilities(i);
                string deviceText = string.Format("{0}, {1} channels", deviceInfo.ProductName, deviceInfo.Channels);
                cbPlaybackDevices.Items.Add(deviceText);
            }
            cbPlaybackDevices.SelectedIndex = 0;
            SpeakerPlayback.SelectedDevice = cbPlaybackDevices.SelectedIndex - 1;

            recorder = new MicrophoneRecorder(protocol);
            int recorderDeviceCount = NAudio.Wave.WaveIn.DeviceCount;
            for (int i = 0; i < recorderDeviceCount; i++)
            {
                NAudio.Wave.WaveInCapabilities deviceInfo = NAudio.Wave.WaveIn.GetCapabilities(i);
                string deviceText = string.Format("{0}, {1} channels", deviceInfo.ProductName, deviceInfo.Channels);
                cbRecordingDevices.Items.Add(deviceText);
            }
            if (recorderDeviceCount > 0)
            {
                MicrophoneRecorder.SelectedDevice = 0;
                cbRecordingDevices.SelectedIndex = 0;
            }

            numVoiceDetectorThreshold.Value = Convert.ToDecimal(recorder.VoiceDetectionThreshold)*100;
        }

        UserInfo GetUserInfo(User user)
        {
            return new UserInfo
            {
                Id = user.Id,
                Deaf = user.Deaf,
                Muted = user.Muted,
                SelfDeaf = user.SelfDeaf,
                SelfMuted = user.SelfMuted,
                Supress = user.Suppress,
                Channel = user.Channel.Id
            };
        }
        ChannelInfo GetChannelInfo(Channel channel)
        {
            return new ChannelInfo
            {
                Name = channel.Name,
                Id = channel.Id,
                Parent = channel.Parent
            };
        }
        TreeNode GetUserNode(uint user_id, TreeNode rootNode)
        {
            foreach (TreeNode node in rootNode.Nodes)
            {
                if (node is TreeNode<UserInfo>) if (((TreeNode<UserInfo>)node).Value.Id == user_id)
                        return node;
                if (node is TreeNode<ChannelInfo>)
                {
                    TreeNode subNode = GetUserNode(user_id, node);
                    if (subNode != null) return subNode;
                }
            }

            return null;
        }
        TreeNode GetChannelNode(uint channel_id, TreeNode rootNode)
        {
            if (rootNode is TreeNode<ChannelInfo>)
                if (((TreeNode<ChannelInfo>)rootNode).Value.Id == channel_id)
                    return rootNode;

            foreach (TreeNode node in rootNode.Nodes)
            {
                if (node is TreeNode<ChannelInfo>)
                {
                    if (((TreeNode<ChannelInfo>)node).Value.Id == channel_id)
                        return node;

                    TreeNode subNode = GetChannelNode(channel_id, node);
                    if (subNode != null) return subNode;
                }
            }

            return null;
        }
        TreeNode MakeChannelNode(Channel channel)
        {
            TreeNode<ChannelInfo> result = new TreeNode<ChannelInfo>();
            result.Text = channel.Name;
            result.BackColor = Color.LightBlue;
            result.Value = GetChannelInfo(channel);

            return result;
        }
        TreeNode MakeUserNode(User user)
        {
            TreeNode<UserInfo> result = new TreeNode<UserInfo>();
            result.Text = user.Name;
            result.BackColor = Color.LightGreen;
            result.Value = GetUserInfo(user);

            return result;
        }
        bool DeleteUserNode(uint user_id, TreeNode rootNode)
        {
            TreeNode<UserInfo> user = null;

            foreach (TreeNode node in rootNode.Nodes)
            {
                if (node is TreeNode<UserInfo>) if (((TreeNode<UserInfo>)node).Value.Id == user_id)
                    {
                        user = node as TreeNode<UserInfo>;
                        break;
                    }
                if (node is TreeNode<ChannelInfo>)
                {
                    if (DeleteUserNode(user_id, node))
                        return true;
                }
            }

            if (user != null)
            {
                user.Remove();
                return true;
            }

            return false;

            return false;
        }
        bool DeleteChannelNode(uint channel_id, TreeNode rootNode)
        {
            if (rootNode is TreeNode<ChannelInfo>) if (((TreeNode<ChannelInfo>)rootNode).Value.Id == channel_id)
                {
                    rootNode.Remove();
                    return true;
                }

            TreeNode channelNode = null;

            foreach (TreeNode node in rootNode.Nodes)
            {
                if (node is TreeNode<ChannelInfo>)
                {
                    if (((TreeNode<ChannelInfo>)node).Value.Id == channel_id)
                    {
                        channelNode = node;
                        break;
                    }

                    if (DeleteUserNode(channel_id, node))
                        return true;
                }
            }

            if (channelNode != null)
            {
                channelNode.Remove();
                return true;
            }
            return false;
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            string message = tbSendMessage.Text;
            Channel target = protocol.LocalUser.Channel;
            tbLog.BeginInvoke((MethodInvoker)(() =>
            {
                tbLog.AppendText(string.Format("[{0:HH:mm:ss}] {1} to {2}: {3}\n", DateTime.Now, protocol.LocalUser.Name, protocol.LocalUser.Channel.Name, message));
            }));

            var msg = new MumbleProto.TextMessage
            {
                Actor = protocol.LocalUser.Id,
                Message = tbSendMessage.Text,
            };
            if (msg.ChannelIds == null)
                msg.ChannelIds = new uint[] { target.Id };
            else
                msg.ChannelIds = msg.ChannelIds.Concat(new uint[] { target.Id }).ToArray();

            connection.SendControl<MumbleProto.TextMessage>(MumbleSharp.Packets.PacketType.TextMessage, msg);
            tbSendMessage.Text = "";
        }

        private void mumbleUpdater_Tick(object sender, EventArgs e)
        {
            if (connection != null)
                if (connection.Process())
                    Thread.Yield();
                else
                    Thread.Sleep(1);
        }

        private void tvUsers_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (tvUsers.SelectedNode is TreeNode<ChannelInfo>)
            {
                ChannelInfo channel = ((TreeNode<ChannelInfo>)tvUsers.SelectedNode).Value;
                //Enter that channel, needs the functionality in connection or protocol.

                protocol.Channels.SingleOrDefault(a => a.Id == channel.Id)?.Join();
            }
        }

        //--------------------------

        void EncodedVoiceDelegate(BasicMumbleProtocol proto, byte[] data, uint userId, long sequence, MumbleSharp.Audio.Codecs.IVoiceCodec codec, MumbleSharp.Audio.SpeechTarget target)
        {
            User user = proto.Users.FirstOrDefault(u => u.Id == userId);
            AddPlayback(user);

            TreeNode<UserInfo> userNode = null;
            foreach (TreeNode<ChannelInfo> chanelNode in tvUsers.Nodes)
            {
                foreach (TreeNode<UserInfo> subNode in chanelNode.Nodes.OfType<TreeNode<UserInfo>>())
                {
                    if (subNode.Value.Id == user.Id)
                        userNode = (TreeNode<UserInfo>)subNode;
                }

                if (userNode != null)
                {
                    break;
                }
            }

            if (userNode != null)
            {
                tvUsers.AddNotifyingNode(userNode, " [SPEAK]", TimeSpan.FromMilliseconds(500));
            }
        }

        void ChannelJoinedDelegate(BasicMumbleProtocol proto, Channel channel)
        {
            TreeNode<ChannelInfo> channelNode = null;
            if (tvUsers.Nodes.Count > 0)
                channelNode = (TreeNode<ChannelInfo>)GetChannelNode(channel.Id, tvUsers.Nodes[0]);

            if (channelNode == null)
            {
                channelNode = (TreeNode<ChannelInfo>)MakeChannelNode(channel);

                TreeNode<ChannelInfo> channeParentlNode = null;
                if (channel.Id > 0)
                {
                    if (tvUsers.Nodes.Count > 0)
                        channeParentlNode = (TreeNode<ChannelInfo>)GetChannelNode(channel.Parent, tvUsers.Nodes[0]);
                }

                if (channeParentlNode == null)
                    tvUsers.Nodes.Add(channelNode);
                else
                    channeParentlNode.Nodes.Add(channelNode);
            }
        }
        void ChannelLeftDelegate(BasicMumbleProtocol proto, Channel channel)
        {
            DeleteChannelNode(channel.Id, tvUsers.Nodes[0]);
        }
        void UserJoinedDelegate(BasicMumbleProtocol proto, User user)
        {
            TreeNode<UserInfo> userNode = (TreeNode<UserInfo>)MakeUserNode(user);
                
                TreeNode channelNode = GetChannelNode(user.Channel.Id, tvUsers.Nodes[0]);
                if (channelNode == null)
                {
                    channelNode = MakeChannelNode(user.Channel);

                    TreeNode parentChannelNode = GetChannelNode(user.Channel.Parent, tvUsers.Nodes[0]);
                    parentChannelNode.Nodes.Add(channelNode);
                }
                channelNode.Nodes.Add(userNode);
            }

        void UserStateChangedDelegate(BasicMumbleProtocol proto, User user)
        {
            TreeNode<UserInfo> userNode = (TreeNode<UserInfo>)GetUserNode(user.Id, tvUsers.Nodes[0]);

            if (userNode == null)
            {
                //Just for safety:
                //this should never happen as the UserJoinedDelegate should have already been called
                //therefore the userNode should always exist when UserStateChangedDelegate is called
                UserJoinedDelegate(proto, user);
            }
            else
            {
                userNode.Value = GetUserInfo(user);
            }
        }

        void UserStateChannelChangedDelegate(BasicMumbleProtocol proto, User user, uint oldChannelId)
                {
            TreeNode<UserInfo> userNode = (TreeNode<UserInfo>)GetUserNode(user.Id, tvUsers.Nodes[0]);

            GetChannelNode(oldChannelId, tvUsers.Nodes[0])
                .Nodes.Remove(userNode);

            GetChannelNode(user.Channel.Id, tvUsers.Nodes[0])
                .Nodes.Add(userNode);
                }

        private void AddPlayback(User user)
        {
            if (user.Id != connection.Protocol.LocalUser.Id)
                SpeakerPlayback.AddPlayer(user.Id, user.Voice);
        }
        void UserLeftDelegate(BasicMumbleProtocol proto, User user)
        {
            DeleteUserNode(user.Id, tvUsers.Nodes[0]);

            SpeakerPlayback.RemovePlayer(user.Id);
        }
        void ChannelMessageReceivedDelegate(BasicMumbleProtocol proto, ChannelMessage message)
        {
            if (message.Channel.Equals(proto.LocalUser.Channel))
                tbLog.BeginInvoke((MethodInvoker)(() =>
                {
                    tbLog.AppendText(string.Format("[{0:HH:mm:ss}] {1} to {2}: {3}\n", DateTime.Now, message.Sender.Name, message.Channel.Name, message.Text));
                }));
        }
        void PersonalMessageReceivedDelegate(BasicMumbleProtocol proto, PersonalMessage message)
        {
            tbLog.BeginInvoke((MethodInvoker)(() =>
            {
                tbLog.AppendText(string.Format("[{0:HH:mm:ss}] {1} to you: {2}\n", DateTime.Now, message.Sender.Name, message.Text));
            }));
        }

        void ServerConfigDelegate(BasicMumbleProtocol proto, MumbleProto.ServerConfig serverConfig)
        {
            tbLog.BeginInvoke((MethodInvoker)(() =>
            {
                tbLog.AppendText(string.Format("{0}\n", serverConfig.WelcomeText));
            }));
        }

        private void btnRecord_Click(object sender, EventArgs e)
        {
            if (recorder._recording)
            {
                btnRecord.Text = "record";
                recorder.Stop();
            }
            else
            {
                btnRecord.Text = "stop";
                recorder.Record();
            }
        }

        private void tvUsers_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {

        }

        private void tvUsers_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
        {

        }

        private void cbPlaybackDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            SpeakerPlayback.SelectedDevice = cbPlaybackDevices.SelectedIndex - 1;
        }

        private void cbRecordingDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            MicrophoneRecorder.SelectedDevice = cbRecordingDevices.SelectedIndex;
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            string name = textBoxUserName.Text;
            string pass = textBoxUserPassword.Text;
            int port;
            string addr;

            if(textBoxServer.Text.Contains(':'))
            {
                var server = textBoxServer.Text.Split(':');
                addr = server[0];
                port = Int32.Parse(server[1]);
            }
            else
            {
                addr = textBoxServer.Text;
                port = 64738;
            }

            if (connection != null)
            {
                connection.Close();
                connection = null;
                protocol.Close();
                tvUsers.Nodes.Clear();
            }

            string srvConnectName = textBoxUserName.Text + "@" + addr + ":" + port;

            connection = new MumbleConnection(new IPEndPoint(Dns.GetHostAddresses(addr).First(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork), port), protocol);
            connection.Connect(name, pass, new string[0], srvConnectName);

            while (connection.Protocol.LocalUser == null)
            {
                connection.Process();
                Thread.Sleep(1);
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            if (connection != null)
            {
                connection.Close();
                connection = null;
                protocol.Close();
                tvUsers.Nodes.Clear();
            }
        }

        private void numVoiceDetectionThreshold_ValueChanged(object sender, EventArgs e)
        {
            recorder.VoiceDetectionThreshold = Convert.ToSingle(numVoiceDetectorThreshold.Value / 100);
        }

        //----------------------------
    }
}
