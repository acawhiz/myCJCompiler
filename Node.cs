using System;

public class Node
{
    //private Node head=null;
    public Node left=null;
    public Node right=null;
    private string data=null;
    public ushort address = 0;//If node is an operator it point to location where value is stored 
    public int codeblock_assignment = 0;//keep track of code blocks to handle jumps
    public int codeblock_lookahead_else = 0;//keep track of code blocks to handle jumps if else exist
    public Node(/*Node head,*/ string data)
    {
        //this.head = head;
        this.data = data;
    }

    public Node(Node node )//added 6/21 for test
    {
        //this.head = head;
        this.data = node.data;
        this.left = node.left;
        this.right = node.right;
    }

    public string getValue()
    {
        return this.data;
    }
    public void setValue(string data)
    {
        this.data = data;
    }
    public void addNodeLeft( string data)
    {
        new Node( data);
    }
    public void addNodeRight(  string data)
    {
        new Node( data);
    }
}
