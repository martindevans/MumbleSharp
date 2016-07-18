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
        private class AudioPlayer
        {
            private readonly NAudio.Wave.WaveOut _playbackDevice = new NAudio.Wave.WaveOut();

            public AudioPlayer(NAudio.Wave.IWaveProvider provider)
            {
                _playbackDevice.Init(provider);
                _playbackDevice.Play();

                _playbackDevice.PlaybackStopped += (sender, args) =>
                    {
                        //MessageBox.Show("stop");
                        //Console.WriteLine("Playback stopped: " + args.Exception);
                    };
            }
        }
        readonly Dictionary<User, AudioPlayer> _players = new Dictionary<User, AudioPlayer>(); 

        MumbleConnection connection;
        ConnectionMumbleProtocol protocol;
        MicrophoneRecorder recorder;

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
            string name = "TestClient2";
            string pass = "";
            int port = 64738;
            string addr = "localhost";

            InitializeComponent();

            protocol = new ConnectionMumbleProtocol();
            protocol.channelMessageReceivedDelegate = ChannelMessageReceivedDelegate;
            protocol.personalMessageReceivedDelegate = PersonalMessageReceivedDelegate;
            protocol.encodedVoice = EncodedVoiceDelegate;
            protocol.userJoinedDelegate = UserJoinedDelegate;
            protocol.userLeftDelegate = UserLeftDelegate;
            protocol.serverConfigDelegate = ServerConfigDelegate;

            connection = new MumbleConnection(new IPEndPoint(Dns.GetHostAddresses(addr).First(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork), port), protocol);
            connection.Connect(name, pass, new string[0], addr);
            
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

                if (!_players.ContainsKey(user))
                    _players.Add(user, new AudioPlayer(user.Voice));
                else _players[user] = new AudioPlayer(user.Voice);
            }
            ChannelTree RootChannel = channels[0];
            
            tvUsers.Nodes.Add(MakeNode(RootChannel));
            tvUsers.ExpandAll();

            recorder = new MicrophoneRecorder(protocol);

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

        private void btnSend_Click(object sender, EventArgs e)
        {
            Channel target = protocol.LocalUser.Channel;
            //if (tvUsers.SelectedNode != null && tvUsers.SelectedNode is TreeNode<Channel>)
            //{
            //    target = ((TreeNode<Channel>)tvUsers.SelectedNode).Value; //This does not seem to work.
            //}

            var msg = new MumbleProto.TextMessage
            {
                actor = protocol.LocalUser.Id,
                message = tbSendMessage.Text,
            };
            msg.channel_id.Add(target.Id);
            //msg.session = 0;
            //msg.tree_id = 0;

            connection.SendControl<MumbleProto.TextMessage>(MumbleSharp.Packets.PacketType.TextMessage, msg);
            tbSendMessage.Text = "";

            //MumbleSharp.Extensions.IEnumerableOfChannelExtensions.SendMessage();
        }

        private void mumbleUpdater_Tick(object sender, EventArgs e)
        {
            connection.Process();

            //foreach (TreeNode<Channel> chanelNode in tvUsers.Nodes)
            //{
            //    foreach (TreeNode<User> subNode in chanelNode.Nodes)
            //        subNode.Text = subNode.Value.Name;
            //}
        }

        private void tvUsers_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (tvUsers.SelectedNode is TreeNode<Channel>)
            {
                Channel channel = ((TreeNode<Channel>)tvUsers.SelectedNode).Value;
                //Enter that channel, needs the functionality in connection or protocol.
            }
        }

        //--------------------------

        void EncodedVoiceDelegate(BasicMumbleProtocol proto, byte[] data, uint userId, long sequence, MumbleSharp.Audio.Codecs.IVoiceCodec codec, MumbleSharp.Audio.SpeechTarget target)
        {
            User user = proto.Users.FirstOrDefault(u => u.Id == userId);
            TreeNode<User> userNode = null;
            foreach (TreeNode<Channel> chanelNode in tvUsers.Nodes)
            {
                foreach (TreeNode<User> subNode in chanelNode.Nodes)
                    if (subNode.Value.Id == user.Id)
                        userNode = subNode;

                if (userNode != null)
                {
                    break;
                }
            }

            if (userNode != null)
            {
                //userNode.BeginInvoke((MethodInvoker)(() =>
                //    {
                //        userNode.Text = user.Name + " [SPEAK]";
                //    }));
            }


        }
        void UserJoinedDelegate(BasicMumbleProtocol proto, User user)
        {
            TreeNode<Channel> chanelNode = null;
            foreach (TreeNode<Channel> node in tvUsers.Nodes)
                if (node.Value.Id == user.Channel.Id)
                    chanelNode = node;

            if (chanelNode != null)
            {
                TreeNode<User> newNode = new TreeNode<User>();
                newNode.Text = user.Name;
                newNode.BackColor = Color.LightGreen;
                newNode.Value = user;
                chanelNode.Nodes.Add(newNode);
            }

            //TreeNode node = tvUsers.Nodes[0];
            //listBox1.BeginInvoke((MethodInvoker)(() =>
            //{
            //    listBox1.Items.Add(user.Name);
            //}));

            if (!_players.ContainsKey(user))
                _players.Add(user, new AudioPlayer(user.Voice));
            else 
                _players[user] = new AudioPlayer(user.Voice);
        }
        void UserLeftDelegate(BasicMumbleProtocol proto, User user)
        {
            TreeNode<Channel> userChanelNode = null;
            TreeNode<User> userNode = null;
            foreach (TreeNode<Channel> chanelNode in tvUsers.Nodes)
            {
                foreach (TreeNode<User> subNode in chanelNode.Nodes)
                    if (subNode.Value.Id == user.Id)
                        userNode = subNode;

                if (userNode != null)
                {
                    userChanelNode = chanelNode;
                    break;
                }
            }

            if (userChanelNode != null && userNode != null)
            {
                userChanelNode.Nodes.Remove(userNode);
            }

            //tbLog.BeginInvoke((MethodInvoker)(() =>
            //{
            //    tbLog.Items.Remove(user.Name);
            //}));
            //
            _players.Remove(user);
        }
        void ServerConfigDelegate(BasicMumbleProtocol proto, MumbleProto.ServerConfig serverConfig)
        {
            tbLog.BeginInvoke((MethodInvoker)(() =>
            {
                tbLog.AppendText(string.Format("{0}\n", serverConfig.welcome_text));
            }));
        }
        void ChannelMessageReceivedDelegate(BasicMumbleProtocol proto, ChannelMessage message)
        {
            if (message.Channel.Equals(proto.LocalUser.Channel))
                tbLog.BeginInvoke((MethodInvoker)(() =>
                {
                    tbLog.AppendText(string.Format("{0} (channel message): {1}\n", message.Sender.Name, message.Text));
                }));
        }
        void PersonalMessageReceivedDelegate(BasicMumbleProtocol proto, PersonalMessage message)
        {
            tbLog.BeginInvoke((MethodInvoker)(() =>
            {
                tbLog.AppendText(string.Format("{0} (personal message): {1}\n", message.Sender.Name, message.Text));
            }));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (recorder._recording)
            {
                button1.Text = "record";
                recorder.Stop();
            }
            else
            {
                button1.Text = "stop";
                recorder.Record();
            }
        }

        //----------------------------
    }
}
