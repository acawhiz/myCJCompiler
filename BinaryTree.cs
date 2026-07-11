using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using static System.Runtime.InteropServices.JavaScript.JSType;
//https://www.youtube.com/watch?v=Hb0ROzKywW4
//https://www.geeksforgeeks.org/bitwise-operators-in-c-cpp/


///<summary>
///Class:BinaryTree used class Node
///Generate arithmetic binary tree
///</summary>
public class BinaryTree
{
	public Node root = null;
    public int size = 0;
    public int scope=0;
    //private bool NOT_active=false;
    Stack<Node> stack = new Stack<Node>();//handles parenthesis

	public BinaryTree()
	{
		root = null;
	}

    ///<summary>
    ///used for structures if,while for etc
    ///externall created binary tree is passed
    ///</summary>

    public BinaryTree(Node root, int scope)
    {
        this.root = root;
        this.scope = scope;
    }

    ///<summary>
    ///<paramref name="data"/>
    ///Create binary tree in following order
    ///Root is created with no value left node is added then root node gets a value then right node added
    ///if next root is full(left +right not null) creates a new root and add previous root to the left node then right node
    ///then repeats
    ///</summary>
    public void addNode(string data)//temporary to handle ! and ^ singe operand ops
    {
        //Node newNode = new Node(data);
        if (root == null)//if no nodes yet create root(operator nodes) and add left node
        {
            if (stack.Count != 0)
            {
                    root = new Node("");
                    root.left = new Node(data);
                    size += 2;
            }
            else
            {
               // original
               root = new Node("");
               root.left = new Node(data);
               size += 2;
            }
        }
        else
        {


            if (root.getValue().Equals(""))//add (operator) data to current root node
            {
                root.setValue(data);
            }
            else if (root.left != null & root.right == null)//add right node given left is taken
            {
                root.right = new Node(data);
                size++;
            }
            else if (root.left != null & root.right != null)//add new root given left and right is taken from current root
            {
                Node topNode = new Node(data);
                size++;
                topNode.left = root;
                root = topNode;
            }

        }
    }

    

    public void open_par()
    {

        if(root == null)
        {
            root = new Node("");
            stack.Push(root);
            root = null;
        }
        else
        {
            stack.Push(root);
            root = null;
        }

    }


    public void close_par()
    {

        if (stack.Count != 0)
        {
            Node tempNode = root;
            root = stack.Pop();
            if (root.left == null)
                root.left = tempNode;
            else
                root.right = tempNode;

            tempNode = null;
        }
    }


}
