using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextPredictor
{
    public class Predictor
    {
        private readonly Node _predictory = new Node(':'); 

        private class Node
        {
            public char C { get; private set; }
            public int Freq { get; set; }
            public readonly Dictionary<char, Node> Children;
            public int TerminalFreq { get; set; }
            public Node(char c)
            {
                C = c;
                Freq = 0;
                Children = new Dictionary<char, Node>();
                TerminalFreq = 0;
            }
        }

        public void AddWord(string word)
        {
            if (string.IsNullOrEmpty(word)) return;

            var node = _predictory;
            foreach(var c in word)
            {
                if (node.Children.ContainsKey(c))
                {
                    node = node.Children[c];
                    node.Freq++;
                }
                else
                {
                    var newNode = new Node(c);
                    node.Children[c] = newNode;
                    node = newNode;
                }
            }
            node.TerminalFreq++;
        }

        public void RemoveWord(string word)
        {
            if (string.IsNullOrWhiteSpace(word)) return;
            var node = _predictory;
            foreach (var c in word)
            {
                if (!node.Children.ContainsKey(c)) return;
                node = node.Children[word[0]];
                node.Freq--;
            }
            node.TerminalFreq--;
        }

        private static Node GetNext(Dictionary<char, Node> children)
        {
            if (!children.Any()) return null;
            var max = children.First().Value;
            foreach (var child in children.Values)
            {
                if (child.Freq > max.Freq)
                    max = child;
            }
            return max;
        }

        private static Node GetNextNode(Node node, char c, string sub)
        {
            Node lNode = null;
            Node uNode = null;
            if (node.Children.ContainsKey(char.ToLower(c)))
                lNode = node.Children[char.ToLower(c)];
            if (node.Children.ContainsKey(char.ToUpper(c)))
                uNode = node.Children[char.ToUpper(c)];

            if (lNode == null && uNode == null) return null;
            if (lNode == null || uNode == null) return lNode ?? uNode;
            if (sub.Length == 0) return uNode.Freq >= lNode.Freq ? uNode : lNode;

            var lSubNode = GetNextNode(lNode, sub[0], sub.Substring(1));
            var uSubNode = GetNextNode(uNode, sub[0], sub.Substring(1));
            if(lSubNode == null && uSubNode == null) return null;
            if(lSubNode == null || uSubNode == null) return lSubNode ?? uSubNode;
            return uSubNode.Freq >= lSubNode.Freq ? uSubNode : lSubNode;
        }

        public string GetPrediction(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                return null;

            var prediction = new StringBuilder();
            var node = _predictory;

            for (var i = 0; i < prefix.Length; i++)
            {
                var c = prefix[i];
                node = GetNextNode(node, c, prefix.Substring(i));
                if (node == null) return null;
                prediction.Append(node.C);
            }

            var nextNode = GetNext(node.Children);
            while (nextNode != null && nextNode.Freq > node.TerminalFreq)
            {
                node = nextNode;
                prediction.Append(node.C);
                nextNode = GetNext(node.Children);
            }
            return prediction.ToString();
        }

        public string GetSmartPrediction(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix)) return null;
            var retval = GetPrediction(prefix);
            if (!string.IsNullOrEmpty(retval)) return retval;
            retval = GetPrediction("The " + prefix);
            if (!string.IsNullOrEmpty(retval)) return retval;
            retval = GetPrediction("A " + prefix);
            if (!string.IsNullOrEmpty(retval)) return retval;
            return null;
        }
    }
}
