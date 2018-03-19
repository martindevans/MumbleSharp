using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MumbleGuiClient
{
    internal class ChannelsTreeView : TreeView
    {
        private Timer updateTimer = new Timer();
        private List<TreeNode> blinkingNodes = new List<TreeNode>();
        private Dictionary<TreeNode,Tuple<DateTime,string,TimeSpan>> notifyingNodes = new Dictionary<TreeNode, Tuple<DateTime, string, TimeSpan>>();
        public ChannelsTreeView()
        {
            updateTimer.Interval = 200;
            updateTimer.Tick += new EventHandler(t_Tick);
        }
        bool isNodeBlinked = false;
        void t_Tick(object sender, EventArgs e)
        {
            foreach (TreeNode tn in blinkingNodes)
            {
                if (isNodeBlinked)
                {
                    //update Icon
                    tn.Text = tn.Text.Substring(0, tn.Text.Length - 1);//to test
                    isNodeBlinked = false;
                }
                else
                {
                    //update Icon
                    tn.Text = tn.Text + "*";//to test
                    isNodeBlinked = true;
                }
            }

            List<TreeNode> endNotifyingNodes = new List<TreeNode>();
            foreach (KeyValuePair<TreeNode, Tuple<DateTime, string, TimeSpan>> kvp in notifyingNodes)
            {
                if (DateTime.Now > kvp.Value.Item1 + kvp.Value.Item3)
                    endNotifyingNodes.Add(kvp.Key);
            }
            foreach (TreeNode endNotifyingNode in endNotifyingNodes)
            {
                RemoveNotifyingNode(endNotifyingNode);
            }
        }

        public void AddBlinkNode(TreeNode node)
        {
            blinkingNodes.Add(node);
        }
        public void RemoveBlinkNode(TreeNode node)
        {
            blinkingNodes.Remove(node);
        }
        public void ClearBlinkNodes()
        {
            blinkingNodes.Clear();
        }
        public List<TreeNode> BlinkingNodes
        {
            get { return blinkingNodes; }
        }

        public void AddNotifyingNode(TreeNode node, string notificationMessage, TimeSpan duration)
        {
            if (notifyingNodes.ContainsKey(node))
            {
                notifyingNodes[node] = new Tuple<DateTime, string, TimeSpan>(DateTime.Now, notifyingNodes[node].Item2, duration);
            }
            else
            {
                notifyingNodes.Add(node, new Tuple<DateTime, string, TimeSpan>(DateTime.Now, node.Text, duration));
                node.Text = node.Text + " " + notificationMessage;
            }
        }
        public void RemoveNotifyingNode(TreeNode node)
        {
            if (notifyingNodes.ContainsKey(node))
            {
                node.Text = notifyingNodes[node].Item2;
                notifyingNodes.Remove(node);
            }
        }
        public void ClearNotifyingNodes()
        {
            notifyingNodes.Clear();
        }
        public List<TreeNode> NotifyingNodes
        {
            get { return notifyingNodes.Keys.ToList(); }
        }

        public int UpdateInterval
        {
            get { return updateTimer.Interval; }
            set { updateTimer.Interval = value; }
        }
        public void StartUpdating()
        {
            isNodeBlinked = false;
            updateTimer.Enabled = true;
        }
        public void StopUpdating()
        {
            updateTimer.Enabled = false;
        }
    }
}
