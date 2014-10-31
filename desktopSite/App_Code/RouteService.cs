using System;
using System.IO;
using System.Web;
using System.Collections;
using System.Web.Services;
using System.Web.Services.Protocols;

using System.Data;
using System.Text;
using System.Configuration;
using System.Web.Configuration;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Data.OleDb;
using System.ComponentModel;

/// <summary>
/// Summary description for RouteService
/// </summary>
[WebService(Namespace = "http://cyclevancouver.ubc.ca")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
[System.Web.Script.Services.ScriptService()]
public class RouteService : System.Web.Services.WebService
{

    int[,] FNodeData, TNodeData;///Data sorted by From Node and To Node
    int[,] FNodeIndex, TNodeIndex;//Positions of Fnode and Tnode
    //string[, ,] Street;
    double[,] NodesCoord;//node coordinates
    int[,] NodesPreference;//route selection options


    int MaxNode = 27664;//27664;//28163;//26635;//27636; //maximum node id
    int rowCount = 60289;//60285;//116274;//56776;//56777;//Total number of polyline records, ie the number of rows in route2
    int rowVertCount = 86308;//86317;//89509;//81529;//81531;//Total number of vertices


    string[] StreetName ;//street by street turn names
    int[] StreetLength ;//street length

    int[,] FVertData, FVertIndex;
    double[,] VertCoord;

    private const int colID = 0; //first column
    private const int colFNode = 1; //second column
    private const int colTNode = 2; //third column
    private const int colShpLength = 3;
    //private const int colDir = 4;
    private const int colCat = 5;
    private const int colCount = 6; //# fields used for FNodeData and TNodeData
    private const int colVertCount = 3; 


    public RouteService()
    {

        //Connection to Access database
        String sConn = WebConfigurationManager.ConnectionStrings["cyclingConnectionString"].ConnectionString;
        OleDbConnection cyclingConn = new OleDbConnection(sConn);

        //Selection based on from node
        String strFNode = WebConfigurationManager.ConnectionStrings["cyclingFNodeCommand"].ConnectionString;
        //Selection based on to node
        String strTNode = WebConfigurationManager.ConnectionStrings["cyclingTNodeCommand"].ConnectionString;
        OleDbCommand cyclingFNodeCmd = new OleDbCommand(strFNode, cyclingConn);
        OleDbCommand cyclingTNodeCmd = new OleDbCommand(strTNode, cyclingConn);

        //Find the maximum node ID for holding NodeCounts array
        //String strFNodeMax = WebConfigurationManager.ConnectionStrings["cyclingFNodeMaxCommand"].ConnectionString;
        //String strTNodeMax = WebConfigurationManager.ConnectionStrings["cyclingTNodeMaxCommand"].ConnectionString;

        //OleDbCommand cyclingFNodeMaxCmd = new OleDbCommand(strFNodeMax, cyclingConn);
        //OleDbCommand cyclingTNodeMaxCmd = new OleDbCommand(strTNodeMax, cyclingConn);

        //Select previous F_NodeIndex in the database
        String strFNodeIndexSelect = WebConfigurationManager.ConnectionStrings["cyclingSelectFNodeIndexCommand"].ConnectionString;
        OleDbCommand cyclingFNodeIndexSelectCmd = new OleDbCommand(strFNodeIndexSelect, cyclingConn);

        //Select previous T_NodeIndex in the database
        String strTNodeIndexSelect = WebConfigurationManager.ConnectionStrings["cyclingSelectTNodeIndexCommand"].ConnectionString;
        OleDbCommand cyclingTNodeIndexSelectCmd = new OleDbCommand(strTNodeIndexSelect, cyclingConn);

        String strNodesSelect = WebConfigurationManager.ConnectionStrings["cyclingNodesSelectionCommand"].ConnectionString;
        OleDbCommand cyclingNodesSelectionCommand = new OleDbCommand(strNodesSelect, cyclingConn);


        //Selection based on from node
        String strFVert = WebConfigurationManager.ConnectionStrings["cyclingFVertCommand"].ConnectionString;
        OleDbCommand cyclingFVertCmd = new OleDbCommand(strFVert, cyclingConn);

        //Select previous F_VertIndex in the database
        String strFVertIndexSelect = WebConfigurationManager.ConnectionStrings["cyclingSelectFVertIndexCommand"].ConnectionString;
        OleDbCommand cyclingFVertIndexSelectCmd = new OleDbCommand(strFVertIndexSelect, cyclingConn);


        try
        {
            cyclingConn.Open();
            //OleDbDataReader cyclingReader = cyclingFNodeCmd.ExecuteReader();
            //while (cyclingReader.Read())//Total number of records
            //{
            //    rowCount += 1;
            //}
            //cyclingReader.Close();


            //MaxNode = 0; //maximum node id, including from and to nodes
            //int MaxFNodeID = (int)cyclingFNodeMaxCmd.ExecuteScalar(); //Maximum fnode id
            //int MaxTNodeID = (int)cyclingTNodeMaxCmd.ExecuteScalar();//Maximum tnode id
            ////Maximum node id
            //MaxNode = MaxFNodeID;
            //if (MaxTNodeID > MaxFNodeID)
            //{
            //    MaxNode = MaxTNodeID;
            //}

            //Data sorted by FNode
            int i = 1;
            OleDbDataReader ReaderFNodeData = cyclingFNodeCmd.ExecuteReader();
            //int colNodeCount = ReaderFNodeData.FieldCount;
            FNodeData = new int[rowCount + 1, colCount]; // Rows starts from 1, not 0
            StreetName = new string[rowCount + 1];
            StreetLength = new int[rowCount + 1];

            int ttt=0;
            while (ReaderFNodeData.Read())
            {
                for (int k = 0; k < colCount; k++)
                {
                    ttt = Convert.ToInt32(ReaderFNodeData[k]);
                    FNodeData[i, k] = Convert.ToInt32(ReaderFNodeData[k]);
                }
                StreetLength[i] = Convert.ToInt32(ReaderFNodeData[colShpLength]); //shape length
                //Street[FNodeData[i, colFNode], FNodeData[i, colTNode], 1] = Convert.ToString(ReaderFNodeData[4]); //direction
                StreetName[i] = Convert.ToString(ReaderFNodeData[6]); //street name
                i++;

            }
            ReaderFNodeData.Close();

            //Data sorted by TNode
            i = 1;
            OleDbDataReader ReaderTNodeData = cyclingTNodeCmd.ExecuteReader();
            TNodeData = new int[rowCount + 1, colCount];

            while (ReaderTNodeData.Read())
            {

                for (int k = 0; k < colCount; k++)
                {
                    TNodeData[i, k] = Convert.ToInt32(ReaderTNodeData[k]);
                }
                i++;

            }
            ReaderTNodeData.Close();

            //first row NodeIndex[0, node] for position in sorted NodeData
            //second row NodeIndex[1,node] for # of counts of that node
            FNodeIndex = new int[2, MaxNode + 1];
            TNodeIndex = new int[2, MaxNode + 1];

            int tmpVal = 0;
            OleDbDataReader cyclingFNodeIndexSelectReader = cyclingFNodeIndexSelectCmd.ExecuteReader();
            while (cyclingFNodeIndexSelectReader.Read())
            {
                tmpVal = Convert.ToInt32(cyclingFNodeIndexSelectReader[0]);
                FNodeIndex[0, tmpVal] = Convert.ToInt32(cyclingFNodeIndexSelectReader[1]);
                FNodeIndex[1, tmpVal] = Convert.ToInt32(cyclingFNodeIndexSelectReader[2]);
            }

            OleDbDataReader cyclingTNodeIndexSelectReader = cyclingTNodeIndexSelectCmd.ExecuteReader();
            while (cyclingTNodeIndexSelectReader.Read())
            {
                tmpVal = Convert.ToInt32(cyclingTNodeIndexSelectReader[0]);
                TNodeIndex[0, tmpVal] = Convert.ToInt32(cyclingTNodeIndexSelectReader[1]);
                TNodeIndex[1, tmpVal] = Convert.ToInt32(cyclingTNodeIndexSelectReader[2]);
            }

            //node coordinates
            NodesCoord = new double[2, MaxNode + 1];
            //selection preferences: elevation, NO2, vegetation...
            NodesPreference = new int[4, MaxNode + 1];
            OleDbDataReader cyclingNodesSelectionReader = cyclingNodesSelectionCommand.ExecuteReader();
            while (cyclingNodesSelectionReader.Read())
            {
                tmpVal = Convert.ToInt32(cyclingNodesSelectionReader[0]);
                NodesCoord[0, tmpVal] = Convert.ToDouble(cyclingNodesSelectionReader[1]); //Xcoord
                NodesCoord[1, tmpVal] = Convert.ToDouble(cyclingNodesSelectionReader[2]); //Ycoord
                NodesPreference[1, tmpVal] = Convert.ToInt32(cyclingNodesSelectionReader[3]); //NO2
                NodesPreference[2, tmpVal] = Convert.ToInt32(cyclingNodesSelectionReader[4]); //Elev
                NodesPreference[3, tmpVal] = Convert.ToInt32(cyclingNodesSelectionReader[5]); //VegCover %
            }

            ///////////////////Starts here for vertex/////////////////////////////
            i = 1;
            OleDbDataReader ReaderFVertData = cyclingFVertCmd.ExecuteReader();
            FVertData = new int[rowVertCount + 1, colVertCount]; // Rows starts from 1, not 0
            VertCoord = new double[2, rowVertCount + 1];

            ttt = 0;
            while (ReaderFVertData.Read())
            {
                for (int k = 0; k < colVertCount; k++)
                {
                    ttt = Convert.ToInt32(ReaderFVertData[k]);
                    FVertData[i, k] = Convert.ToInt32(ReaderFVertData[k]);
                }
                VertCoord[0, i] = Convert.ToDouble(ReaderFVertData[3]);
                VertCoord[1, i] = Convert.ToDouble(ReaderFVertData[4]);
                i++;

            }
            ReaderFVertData.Close();

            //first row NodeIndex[0, node] for position in sorted NodeData
            //second row NodeIndex[1,node] for # of counts of that node
            FVertIndex = new int[2, MaxNode + 1];

            tmpVal = 0;
            OleDbDataReader cyclingFVertIndexSelectReader = cyclingFVertIndexSelectCmd.ExecuteReader();
            while (cyclingFVertIndexSelectReader.Read())
            {
                tmpVal = Convert.ToInt32(cyclingFVertIndexSelectReader[0]);
                FVertIndex[0, tmpVal] = Convert.ToInt32(cyclingFVertIndexSelectReader[1]);
                FVertIndex[1, tmpVal] = Convert.ToInt32(cyclingFVertIndexSelectReader[2]);
            }
            ///////////////Ends here for vertex/////////////////////////////////////////

        }
        finally
        {
            // Close the connection
            if (cyclingConn != null)
            {
                cyclingConn.Close();
            }
        }


    }

    [WebMethod]
    public void Service_Initialization()
    {
        //This starts the intialization process though no actual code inside.
    }

    //Accept client's option and estimate the optimized route.
    [WebMethod]
    public string NodeCoords(int RouteType, int Preference, int Speed, int Slope, 
        double XcoordStart, double YcoordStart, double XcoordEnd, double YcoordEnd, string txtFile)
    {                        //Preference = 0 for restricted maximum slope, 1 for no2, 2 for elevation, 3 for vege cover and 4 for shortest path.

        //int StartNode = 37539;//UBC
        //int EndNode = 39054;//Coquitlam
        //int EndNode = 5299; //Lanley

        //int StartNode = 34888;//somewhere on Kingsway
        //int EndNode =  33973;//somewhere on Kingsway
        int StartNode = 0;
        int EndNode = 0;

        //Connection to Access database
        String sConn2 = WebConfigurationManager.ConnectionStrings["cyclingConnectionString"].ConnectionString;
        OleDbConnection cyclingConn2 = new OleDbConnection(sConn2);

        try
        {
            cyclingConn2.Open();
            //extend 2 minutes for both long and lat coords to make sure each address has at least one matching node
            double XcoordExtend = 0.01666666667; //for 1 minute
            double YcoordExtend = 0.01666666667; // when using +- Extend, we have 2 minute extension.
            string strStartSelection = "";
            string strEndSelection = "";

            //select all the nodes within the 2 minutes scope
            if (RouteType == 0)
            {
                strStartSelection = "SELECT [NODES_ID], [Xcoord], [Ycoord], [CAT] FROM nodes" +
                                " WHERE ([Xcoord] >=" + (XcoordStart - XcoordExtend) +
                                " and [Xcoord] <=" + (XcoordStart + XcoordExtend) +
                                " and [Ycoord] >= " + (YcoordStart - YcoordExtend) +
                                " and [Ycoord] <=" + (YcoordStart + YcoordExtend) + 
                                " and ([CAT] = 1 or [CAT] = 2) " +
                                ")";
            }
            else if (RouteType == 1) {
                strStartSelection = "SELECT [NODES_ID], [Xcoord], [Ycoord], [CAT] FROM nodes" +
                                " WHERE ([Xcoord] >=" + (XcoordStart - XcoordExtend) +
                                " and [Xcoord] <=" + (XcoordStart + XcoordExtend) +
                                " and [Ycoord] >= " + (YcoordStart - YcoordExtend) +
                                " and [Ycoord] <=" + (YcoordStart + YcoordExtend) +
                                ")";
            }
            OleDbCommand cyclingStartCoordCmd = new OleDbCommand(strStartSelection, cyclingConn2);
            OleDbDataReader cyclingStartCoordReader = cyclingStartCoordCmd.ExecuteReader();

            //keep the mimin distance node as a surrogate for the start address.
            double MinStart = double.MaxValue;
            double Dist = 0;
            while (cyclingStartCoordReader.Read())
            {
                Dist = System.Math.Sqrt((Convert.ToDouble(cyclingStartCoordReader[1]) - XcoordStart) *
                    (Convert.ToDouble(cyclingStartCoordReader[1]) - XcoordStart) +
                    (Convert.ToDouble(cyclingStartCoordReader[2]) - YcoordStart) *
                    (Convert.ToDouble(cyclingStartCoordReader[2]) - YcoordStart));
                if (MinStart > Dist)
                {
                    MinStart = Dist;
                    StartNode = Convert.ToInt32(cyclingStartCoordReader[0]);
                }
            }
            cyclingStartCoordReader.Close();

            //select all the nodes within 2 minutes of the destination address
            if (RouteType == 0)
            {
                strEndSelection = "SELECT [NODES_ID], [Xcoord], [Ycoord] FROM nodes" +
                            " WHERE ([Xcoord] >=" + (XcoordEnd - XcoordExtend) +
                            " and [Xcoord] <=" + (XcoordEnd + XcoordExtend) +
                            " and [Ycoord] >= " + (YcoordEnd - YcoordExtend) +
                            " and [Ycoord] <=" + (YcoordEnd + YcoordExtend) +
                            " and ([CAT] = 1 or [CAT] = 2) " +
                            ")";
            }
            else if (RouteType == 1) {
                strEndSelection = "SELECT [NODES_ID], [Xcoord], [Ycoord], [CAT] FROM nodes" +
                            " WHERE ([Xcoord] >=" + (XcoordEnd - XcoordExtend) +
                            " and [Xcoord] <=" + (XcoordEnd + XcoordExtend) +
                            " and [Ycoord] >= " + (YcoordEnd - YcoordExtend) +
                            " and [Ycoord] <=" + (YcoordEnd + YcoordExtend) +
                            ")";
            }
            OleDbCommand cyclingEndCoordCmd = new OleDbCommand(strEndSelection, cyclingConn2);
            OleDbDataReader cyclingEndCoordReader = cyclingEndCoordCmd.ExecuteReader();

            //keep the mimin distance node as a surrogate for the destination address.
            double MinEnd = double.MaxValue;
            double Dist2 = 0;
            while (cyclingEndCoordReader.Read())
            {
                Dist2 = System.Math.Sqrt((Convert.ToDouble(cyclingEndCoordReader[1]) - XcoordEnd) *
                    (Convert.ToDouble(cyclingEndCoordReader[1]) - XcoordEnd) +
                    (Convert.ToDouble(cyclingEndCoordReader[2]) - YcoordEnd) *
                    (Convert.ToDouble(cyclingEndCoordReader[2]) - YcoordEnd));
                if (MinEnd > Dist2)
                {
                    MinEnd = Dist2;
                    EndNode = Convert.ToInt32(cyclingEndCoordReader[0]);
                }
            }
            cyclingEndCoordReader.Close();
        }
        finally
        {
            // Close the connection
            if (cyclingConn2 != null)
            {
                cyclingConn2.Close();
            }
        }

        int[,] Result = new int[3, MaxNode + 2]; //Save accumulated cost. 
        //first row [0,] = 1 if a node is processed, otherwise =0; 
        //second row [1,] = previous node of shortest path to this node.
        //for the weight of a node
        //      |...col5856 ...|  |1   |    Node 5856 is marked
        //Result|...col5856 ...|  |5000|    The previous node of the shorted path to 5856 is 5000
        //      |...col5856 ...|  |54.6|    Edge cost from start node to 5856 is 54.6
        int FNode = 0;
        int TNode = 0;
        int Sum = int.MaxValue;
        StringBuilder strBuilderNode = new StringBuilder();
        
        StringBuilder strCoordinates = new StringBuilder();
        StringBuilder strKMLCoords = new StringBuilder();
        //string latlong = "Latitude Longitude\r\n";
        //string latlong = "Route_coords <br> Latitude Longitude <br>";
        //strCoordinates.Append(latlong);

        StringBuilder strBuilderStreet = new StringBuilder();
        StringBuilder strBuilderPath = new StringBuilder();
        int Min, Min2;
        int MinLoc = int.MaxValue;
        int i, j, k, m, n;
        int PrNode = 0;
        int TNodeCost = 0;
        int[] path;
        int StartNodeSaved = StartNode;

        for (i = 1; i < MaxNode + 1; i++)
        {
            Result[0, i] = 0;
            Result[2, i] = int.MaxValue;
        }

        Result[0, StartNode] = 1;
        Result[2, StartNode] = 0;
        int t1=0, t2=0, t3=0, t4=0;
        int intLength = 0, tmpGrade = 0;
        double StCatWght;

        for (m = 1; m < MaxNode + 1; m++)
        {
            /////////////////////////////////////////////
            //from node to tnode arcs
            for (i = 0; i < FNodeIndex[1, StartNode]; i++)
            {
                t1 = FNodeIndex[0, StartNode];
                //1: designated, 2: alternate, 3: major road, 4: highway
                if (FNodeData[t1 + i, colCat] == 1) {
                    StCatWght = 1.0;
                }else if (FNodeData[t1 + i, colCat] == 2){
                    StCatWght = 1.3;
                }else if (FNodeData[t1 + i, colCat] == 3){
                    StCatWght = 1.6;
                }
                else if (FNodeData[t1 + i, colCat] == 4)
                {
                    StCatWght = 1.8;
                }
                else {
                    StCatWght = 2.0;                
                }

                
                if(RouteType == 0 && (FNodeData[t1 + i, colCat] == 1 || FNodeData[t1 + i, colCat] == 2))
                {
                
                    TNode = FNodeData[t1 + i, colTNode];
                    t3 = NodesPreference[2, TNode] - NodesPreference[2, StartNode]; //elevation gain
                    if (t3 < 0) { t3 = 0; }
                    intLength = FNodeData[t1 + i, colShpLength];
                    //Assume segment length should be at least 1m; otherwise, tmpGrade can not be calculated.
                    if (intLength < 1)
                    {
                        intLength = 1;
                    }
                    tmpGrade = Convert.ToInt32(t3 * 100 / intLength );
                    if (Preference == 0)//set maximum slope
                    {
                        if ( tmpGrade < Slope)
                        {
                            Sum = Convert.ToInt32(intLength * StCatWght) + Result[2, StartNode];
                        }
                        else {
                            Sum = int.MaxValue;
                        }
                    }
                    else if (Preference == 1)//NO2
                    {
                        Sum = Convert.ToInt32(FNodeData[t1 + i, colShpLength] * Convert.ToInt32((NodesPreference[1, StartNode] + NodesPreference[1, TNode]) / 2.0) * StCatWght) + Result[2, StartNode];

                    }
                    else if (Preference == 2)//Elevation gain
                    {
                        //t3 = NodesPreference[2, TNode]- NodesPreference[2, StartNode];
                        Sum = Convert.ToInt32(t3 * StCatWght) + Result[2, StartNode];
                    }
                    else if (Preference == 3)//vegetation cover
                    {
                        t4 = FNodeData[t1 + i, colShpLength] * (100 - Convert.ToInt32 ((NodesPreference[3, TNode] + NodesPreference[3, StartNode]) / 2.0));
                        Sum = Convert.ToInt32(t4 * StCatWght) + Result[2, StartNode];
                    
                    }
                    else if (Preference == 4)//shortest path route
                    {
                        Sum = Convert.ToInt32(intLength) + Result[2, StartNode];
                    }


                    if (Result[0, TNode] == 0)
                    {
                        Result[2, TNode] = Sum;
                    }
                }
                else if(RouteType == 1)
                {
                
                    TNode = FNodeData[t1 + i, colTNode];
                    t3 = NodesPreference[2, TNode] - NodesPreference[2, StartNode]; //elevation gain
                    if (t3 < 0) { t3 = 0; }
                    intLength = FNodeData[t1 + i, colShpLength];
                    //Assume segment length should be at least 1m; otherwise, tmpGrade can not be calculated.
                    if (intLength < 1)
                    {
                        intLength = 1;
                    }
                    tmpGrade = Convert.ToInt32(t3 * 100 / intLength );
                    if (Preference == 0)//set maximum slope
                    {
                        if ( tmpGrade < Slope)
                        {
                            Sum = Convert.ToInt32(intLength * StCatWght) + Result[2, StartNode];
                        }
                        else {
                            Sum = int.MaxValue;
                        }
                    }
                    else if (Preference == 1)//NO2
                    {
                        Sum = Convert.ToInt32(FNodeData[t1 + i, colShpLength] * Convert.ToInt32((NodesPreference[1, StartNode] + NodesPreference[1, TNode]) / 2.0) * StCatWght) + Result[2, StartNode];

                    }
                    else if (Preference == 2)//Elevation gain
                    {
                        //t3 = NodesPreference[2, TNode]- NodesPreference[2, StartNode];
                        Sum = Convert.ToInt32(t3 * StCatWght) + Result[2, StartNode];
                    }
                    else if (Preference == 3)//vegetation cover
                    {
                        t4 = FNodeData[t1 + i, colShpLength] * (100 - Convert.ToInt32 ((NodesPreference[3, TNode] + NodesPreference[3, StartNode]) / 2.0));
                        Sum = Convert.ToInt32(t4 * StCatWght) + Result[2, StartNode];
                    
                    }
                    else if (Preference == 4)//shortest path route
                    {
                        Sum = Convert.ToInt32(intLength) + Result[2, StartNode];
                    }
                    if (Result[0, TNode] == 0)
                    {
                        Result[2, TNode] = Sum;
                    }
                }

            }
            /////////////////////////////////////////////

            //Pick up the smallest one, but remember the location
            Min = int.MaxValue;
            for (i = 1; i < MaxNode + 1; i++)
            {
                if (Result[0, i] == 0 && Min > Result[2, i])
                {
                    Min = Result[2, i];
                    MinLoc = i;
                }
            }
            if (Min < int.MaxValue)
            {
                Result[0, MinLoc] = 1;
            }

            Min2 = int.MaxValue;
            //find the previous marked node
            for (i = 0; i < TNodeIndex[1, MinLoc]; i++)
            {
                t2 = TNodeIndex[0, MinLoc];
                FNode = TNodeData[t2+ i, colFNode];

                //1: designated, 2: alternate, 3: major road, 4: highway
                if (TNodeData[t2 + i, colCat] == 1)
                {
                    StCatWght = 1.0;
                }
                else if (TNodeData[t2 + i, colCat] == 2)
                {
                    StCatWght = 1.3;
                }
                else if (TNodeData[t2 + i, colCat] == 3)
                {
                    StCatWght = 1.6;
                }
                else if (TNodeData[t2 + i, colCat] == 4)
                {
                    StCatWght = 1.8;
                }
                else {
                    StCatWght = 2.0;                
                }


                if (Preference == 0)//restricted maximum slope
                {
                    if (TNodeData[t2 + i, colShpLength] < 1)
                    {
                        TNodeCost = Convert.ToInt32(1 * StCatWght);
                    }
                    else {
                        TNodeCost = Convert.ToInt32(StCatWght * TNodeData[t2 + i, colShpLength]);                    
                    }
                }
                else if (Preference == 1)//NO2
                {
                    TNodeCost = Convert.ToInt32(TNodeData[t2 + i, colShpLength] * Convert.ToInt32((NodesPreference[1, MinLoc] + NodesPreference[1, FNode])/2.0)* StCatWght);

                }
                else if (Preference == 2)//Elevation gain
                {
                    t3 = NodesPreference[2, MinLoc] - NodesPreference[2, FNode];
                    if (t3 < 0) { t3 = 0; }
                    TNodeCost = Convert.ToInt32(t3 * StCatWght) ;
                }
                else if (Preference == 3)//Vegetation cover
                {
                    TNodeCost = Convert.ToInt32(TNodeData[t2 + i, colShpLength] * (100- Convert.ToInt32(1 - (NodesPreference[3, MinLoc] + NodesPreference[3, FNode]) / 2.0))* StCatWght);
                }
                else if (Preference == 4)//shortest path, no weighting
                {
                    if (TNodeData[t2 + i, colShpLength] < 1)
                    {
                        TNodeCost = Convert.ToInt32(1.0);
                    }
                    else
                    {
                        TNodeCost = Convert.ToInt32(TNodeData[t2 + i, colShpLength]);
                    }
                }



                if (Result[0, FNode] == 1)
                {
                    if (Min2 > Result[2, FNode] + TNodeCost) 
                    {
                        Min2 = Result[2, FNode] + TNodeCost; 
                        PrNode = FNode;
                    }
                }

            }

            Result[1, MinLoc] = PrNode;
            StartNode = MinLoc;
            if (StartNode == EndNode)//If find a route, then stop
            {
                break;
            }
        }
        if (StartNode != EndNode)//there is no route between start and destination addresses
        {
            return "No_path";
        }

        j = EndNode;
        k = 1; //number of nodes

        while (StartNodeSaved != j )
        {
            j = Convert.ToInt32(Result[1, j]);
            k += 1;
        }

        n = k; //number of nodes
        j = EndNode;
        path = new int[n];
        while (StartNodeSaved != j)//sort out route path
        {
            path[k - 1] = j;
            j = Convert.ToInt32(Result[1, j]);
            k -= 1;
        }
        path[0] = j;

        int TotalShpLength = 0;//total route length
        int TotalNO2Conc = 0;//total no2 concentration
        int TotalElevGain = 0;//total elevation gain
        int TotalVegCover = 0;//total vegetation cover
        double TotalHour = 0.0;//time spent in hour
        int intHour = 0;//in hour
        int intMin = 0;//in minute

        string[] Street2 = new string[n] ;
        int[] Length2 = new int[n];
        int elvDiffer = 0;
        int tmpSlope =0, Grade = 1;
        bool FoundMatch = false;
        for (m = 0; m < n-1; m++)
        {
            TotalNO2Conc += NodesPreference[1, path[m]]; //Estimate total NO2 concentrations
            TotalVegCover += NodesPreference[3, path[m]]; //Estimate total vegetation cover
            for (k = 0; k < FNodeIndex[1, path[m]]; k++)
            {
                TNode = FNodeData[FNodeIndex[0, path[m]] + k, colTNode];
                if (TNode == path[m + 1])
                {
                    Street2[m] = StreetName[FNodeIndex[0, path[m]] + k]; //remember street names
                    Length2[m] = StreetLength[FNodeIndex[0, path[m]] + k];//remember route street lengths
                    elvDiffer = NodesPreference[2, path[m + 1]] - NodesPreference[2, path[m]];//estimate elevation difference of a street
                    if (elvDiffer > 0)
                        TotalElevGain += elvDiffer;//total elelvation difference of a route
                    break;
                }
            }

            //make sure a street length at least one meter to avoid time out errors.
            if (Length2[m] < 1)
            {
                Length2[m] = 1;
            }

            //categoreis of slope gradient
            tmpSlope = Convert.ToInt32(elvDiffer * 100 /Length2[m]);
            if (tmpSlope <= 2)
            {
                Grade = 1;
            }
            else if (tmpSlope <= 4)
            {
                Grade = 2;
            }
            else if (tmpSlope <= 6)
            {
                Grade = 3;
            }
            else if (tmpSlope <= 10)
            {
                Grade = 4;
            }
            else if (tmpSlope > 10)
            {
                Grade = 5;
            }

            ///////////////////////////Scenario #1 use nodes but not vertices/////////////////////////////
            //If we use nodes but not vertices, then use the following two lines of code.
            //strBuilderNode.Append(Convert.ToString(NodesCoord[1, path[m]] + "|")); //latitude
            //strBuilderNode.Append(Convert.ToString(NodesCoord[0, path[m]] + " ")); //longitude
            
            
            ///////////////////////////Scenario #2 use vertices but not nodes/////////////////////////////            //If use vertices instead, then use the following code.
            FoundMatch = false;
            if (FVertIndex[1, path[m]]>0 )
            {
                for (k = 0; k < FVertIndex[1, path[m]]; k++ )
                {
                    TNode = FVertData[FVertIndex[0, path[m]]+k, colTNode];
                    if (TNode == path[m + 1])
                    {
                        FoundMatch = true;
                        strBuilderNode.Append(Convert.ToString(VertCoord[1, FVertIndex[0, path[m]] + k] + "|")); //latitude
                        strBuilderNode.Append(Convert.ToString(VertCoord[0, FVertIndex[0, path[m]] + k] + "|")); //longitude
                        strBuilderNode.Append(Convert.ToString(Grade + " ")); //slope gradient

                        strCoordinates.Append(Convert.ToString(VertCoord[1, FVertIndex[0, path[m]] + k] + " ")); //latitude
                        strCoordinates.Append(Convert.ToString(VertCoord[0, FVertIndex[0, path[m]] + k] + "<br>")); //longitude

                        strKMLCoords.Append(Convert.ToString(VertCoord[0, FVertIndex[0, path[m]] + k] + ", " + "|"));//longitude
                        strKMLCoords.Append(Convert.ToString(VertCoord[1, FVertIndex[0, path[m]] + k] + ",0 " + "|")); //latitude
                        strKMLCoords.Append(Convert.ToString(Grade + "%")); //slope gradient

                        

                        
                    }
                }
            }
            if (FoundMatch == false)
            {
                if (FVertIndex[1, path[m+1]] > 0)
                {
                    for (k = FVertIndex[1, path[m+1]]-1; k >= 0; k--)
                    {
                        TNode = FVertData[FVertIndex[0, path[m+1]] + k, colTNode];
                        if (TNode == path[m])
                        {
                            strBuilderNode.Append(Convert.ToString(VertCoord[1, FVertIndex[0, path[m+1]] + k] + "|")); //latitude
                            strBuilderNode.Append(Convert.ToString(VertCoord[0, FVertIndex[0, path[m+1]] + k] + "|")); //longitude
                            strBuilderNode.Append(Convert.ToString(Grade + " ")); //slope gradient

                            strCoordinates.Append(Convert.ToString(VertCoord[1, FVertIndex[0, path[m + 1]] + k] + " ")); //latitude
                            strCoordinates.Append(Convert.ToString(VertCoord[0, FVertIndex[0, path[m + 1]] + k] + "<br>")); //longitude

                            strKMLCoords.Append(Convert.ToString(+VertCoord[0, FVertIndex[0, path[m + 1]] + k] + "," + "|")); //longitude
                            strKMLCoords.Append(Convert.ToString(VertCoord[1, FVertIndex[0, path[m + 1]] + k] + ",0 " + "|")); //latitude
                            strKMLCoords.Append(Convert.ToString(Grade + "%")); //slope gradient
                            
                           }
                    }
                }
            }
            ///////////////////////////Ends scenario #2 code/////////////////////////////

        }
        ///////////////////////////Scenario #1 use nodes but not vertices/////////////////////////////        
        //If we use nodes but not vertices, then use the following two lines of code.
        //strBuilderNode.Append(Convert.ToString(NodesCoord[1, path[n - 1]] + "|")); //latitude
        //strBuilderNode.Append(Convert.ToString(NodesCoord[0, path[n-1]] + " ")); //longitude

        TotalNO2Conc += NodesPreference[1, path[n-1]];
        TotalVegCover += NodesPreference[3, path[n-1]];

            string tmpString = "";
            int tmpLength = 0;
            //int[] StreetLength = new int[n];
            //string[] StreetName = new string[n];

        for (k = 0; k < n-1; k++ )
            {
                TotalShpLength += Length2[k];

                if (k == 0)
                {
                    //StreetName[i] = Street2[k]);
                    strBuilderPath.Append( Street2[k] + "  ("); //Street name
                    tmpString = Street2[k];
                }

                if (Street2[k] == tmpString)
                {
                    tmpLength += Length2[k];
                }
                else
                {
                    //StreetLength[i] = tmpLength;
                    strBuilderPath.Append(Convert.ToString(Convert.ToInt32(tmpLength) + " m)")); //Length of the street.

                    //StreetName[i] = Convert.ToString (cyclingPathSelectionReader[3]);
                    strBuilderPath.Append(" <br> " + Street2[k] + "  (" ); //Street name
                    tmpString = Street2[k];
                    tmpLength = Length2[k];
                }
            }
        //StreetLength[i] = tmpLength;
        strBuilderPath.Append(Convert.ToString(Convert.ToInt32(tmpLength) + " m)")); 
        TotalShpLength = Convert.ToInt32(TotalShpLength);
        
        TotalHour = (TotalShpLength * 0.001 / Speed);

        intHour = Convert.ToInt32(Math.Floor(TotalHour));
        //if TotalHour = 2.62, then ToInt32(TotalHour) = 3
        
        intMin = Convert.ToInt32((TotalHour - intHour) * 6000) / 100;


        ///Export coordinates and slope grades to a text file////////////////////////////////////////////////////////////////
        // Get current date and time
        //DateTime dt = DateTime.Now;

        // Get year, month, and day
        //string year = dt.Year.ToString();
        //string month = dt.Month.ToString();
        //string day = dt.Day.ToString();
        //string hour = dt.Hour.ToString();
        //string minute = dt.Minute.ToString();

        //string path2;
        //path2 = "_" + year + "." + month + "." + day + "." + hour + "." + minute + ".txt";

        try
        {
         //   // Ensure that the target does not exist.
         //   File.Delete("c:\\inetpub\\wwwroot\\Route\\Text\\" + txtFile);
         //
         //   using (TextWriter writer = File.CreateText("c:\\inetpub\\wwwroot\\Route\\Text\\" + txtFile))
         //   {
         //
         //       writer.Write(strCoordinates.ToString().Trim());
         //       writer.Close();
         //   }

            // *** Write to file ***
            String strKML = strKMLCoords.ToString();
            String[] arrKML = strKML.Split(new Char[] { '%' });
            // Specify file, instructions, and privelegdes
            //This is obsolete now, because we do not output the coordinates into a text file
            FileStream file = new FileStream(Server.MapPath("/KMLRoutes/" + txtFile), FileMode.OpenOrCreate, FileAccess.Write);
            // Create a new stream to write to the file
            StreamWriter sw = new StreamWriter(file);
            // Write a string to the file
            sw.WriteLine("<?xml version='1.0' encoding='UTF-8'?><kml xmlns='http://www.opengis.net/kml/2.2'><Document><name>CyclingRoute.kml</name><open>1</open><Style id='style1'><LineStyle><color>7f0058c9</color><width>6</width></LineStyle></Style><Style id='style2'><LineStyle><color>7f00FFFE</color><width>6</width></LineStyle></Style><Style id='style3'><LineStyle><color>7f00EA48</color><width>6</width></LineStyle></Style><Style id='style4'><LineStyle><color>7fffbe00</color><width>6</width></LineStyle></Style><Style id='style5'><LineStyle><color>7fE20000</color><width>6</width></LineStyle></Style>");
            //sw.Write(strKMLCoords.ToString().Trim());  
            string[] latlng1; 
            string[] latlng2;
            int arrKMLCount = arrKML.Length - 2;
            //for (int x = 0; x < arrKMLCount; x++)
            int x = 0;
            sw.WriteLine(arrKMLCount);
            while (x < arrKMLCount)
            {
                latlng1 = arrKML[x].Split(new Char[] { '|' });
                latlng2 = arrKML[x + 1].Split(new Char[] { '|' });
                int kmlGrade = Convert.ToInt32(latlng2[2]);
                if (kmlGrade == 1)
                {
                    sw.WriteLine("<Placemark><styleUrl>#style5</styleUrl><LineString><coordinates>");
                    sw.WriteLine(latlng1[0] + latlng1[1] + latlng2[0] + latlng2[1]);
                    sw.WriteLine("</coordinates></LineString></Placemark>");
                    sw.Flush();
                    x = x + 1;
                }

                if (kmlGrade == 2)
                {
                    sw.WriteLine("<Placemark><styleUrl>#style4</styleUrl><LineString><coordinates>");
                    sw.WriteLine(latlng1[0] + latlng1[1] + latlng2[0] + latlng2[1]);
                    sw.WriteLine("</coordinates></LineString></Placemark>");
                    sw.Flush();
                    x = x + 1;
                }

                if (kmlGrade == 3)
                {
                    sw.WriteLine("<Placemark><styleUrl>#style3</styleUrl><LineString><coordinates>");
                    sw.WriteLine(latlng1[0] + latlng1[1] + latlng2[0] + latlng2[1]);
                    sw.WriteLine("</coordinates></LineString></Placemark>");
                    sw.Flush();
                    x = x + 1;
                }

                if (kmlGrade == 4)
                {
                    sw.WriteLine("<Placemark><styleUrl>#style2</styleUrl><LineString><coordinates>");
                    sw.WriteLine(latlng1[0] + latlng1[1] + latlng2[0] + latlng2[1]);
                    sw.WriteLine("</coordinates></LineString></Placemark>");
                    sw.Flush();
                    x = x + 1;
                }

                if (kmlGrade == 5)
                {
                    sw.WriteLine("<Placemark><styleUrl>#style1</styleUrl><LineString><coordinates>");
                    sw.WriteLine(latlng1[0] + latlng1[1] + latlng2[0] + latlng2[1]);
                    sw.WriteLine("</coordinates></LineString></Placemark>");
                    sw.Flush();
                    x = x + 1;
                }
            } 
            
            sw.Write("</Document></kml>");          
            // Close StreamWriter
            sw.Close();
            // Close file
            file.Close();
            
        }
        catch (Exception e)
        {
            Console.WriteLine("The process failed: {0}", e.ToString());
        }

        ///End exporting data to a text file////////////////////////////////////////////////////////////////////////////////

		//MN edits Summer 2009
        //Export route calculation results
		if (RouteType == 1)
		{
        return strBuilderNode.ToString().Trim() 
			+ "?"
            + "<B> Route Information:</b><hr>"
            + " Route length: <b> " + Convert.ToString(TotalShpLength/1000.0) + " km. </b> "
            //+ " <br> Estimated time: <b> " + Convert.ToString(Convert.ToInt32(TotalShpLength / Speed) / 1000.0) + " hr. </b>"
            + " <br> Estimated time: <b> " + Convert.ToString(intHour) + " hr " + Convert.ToString(intMin) + " min. </b>"

            + " <br> GHG prevented: <b> " + Convert.ToString(Convert.ToInt32(TotalShpLength * 0.25 / 10.0)/100.0) + " kg. </b>" //1km 0.25kg ghg
            + " <br> Calories burned: <b> " + Convert.ToString(Convert.ToInt32(TotalShpLength * 21.75 / 100.0 )/10.0) + " kCal. </b>"
            + " <br> Mean NO2 level: <b> " + Convert.ToString(Convert.ToInt32(TotalNO2Conc / n)) + " ppb. </b> "
            + " <br> Elevation gain: <b> " + Convert.ToString(Convert.ToInt32(TotalElevGain )) + " m. </b> "
            + " <br> Average veg cover: <b> " + Convert.ToString(Convert.ToInt32(TotalVegCover / n )) + " %. </b> "
            + " <br> <img src='images/slope_grade2.png' width='175' /> "
            + "<br><input name='btnPrint' type='submit' value='Printer Friendly Output' onclick='ClickToPrint()';>"
            + "<br><br><input name='btnKML' type='submit' value='Download KML' onclick='downloadKML()';>"
            + "<br><br><input name='btnGPS' type='submit' value='Get GPS Coordinates' onclick='showGPSWindow()';>"
            + "<br><HR><B> Suggested Route:</b>"
            + "<br> <a href='http://www.th.gov.bc.ca/BikeBC/restrictions_map.html#LM' target= '_blank'> Please view the map of roads prohibited to cyclists</a><HR>"
			+ "?"
            + strBuilderPath.ToString().Trim()
            + "?" 
			+ strCoordinates.ToString().Trim();

			//for (m = 0; m < n; m++)
			//{
			//    strBuilderNode.Append(path[m] + " ");
			//}
		

			//while (StartNodeSaved != j)
			//{
			//    if (j == EndNode)
			//    {
			//        strBuilderNode.Append(j + " ");
			//    }
			//    strBuilderNode.Append(Result[1, j] + " ");
			//    j = Result[1, j];
			//}
		}
		else
	
		{return strBuilderNode.ToString().Trim() + "?"
            + "<B> Route Information:</b><hr>"
            + " Route length: <b> " + Convert.ToString(TotalShpLength/1000.0) + " km. </b> "
            //+ " <br> Estimated time: <b> " + Convert.ToString(Convert.ToInt32(TotalShpLength / Speed) / 1000.0) + " hr. </b>"
            + " <br> Estimated time: <b> " + Convert.ToString(intHour) + " hr " + Convert.ToString(intMin) + " min. </b>"

            + " <br> GHG prevented: <b> " + Convert.ToString(Convert.ToInt32(TotalShpLength * 0.25 / 10.0)/100.0) + " kg. </b>" //1km 0.25kg ghg
            + " <br> Calories burned: <b> " + Convert.ToString(Convert.ToInt32(TotalShpLength * 21.75 / 100.0 )/10.0) + " kCal. </b>"
            + " <br> Mean NO2 level: <b> " + Convert.ToString(Convert.ToInt32(TotalNO2Conc / n)) + " ppb. </b> "
            + " <br> Elevation gain: <b> " + Convert.ToString(Convert.ToInt32(TotalElevGain )) + " m. </b> "
            + " <br> Average veg cover: <b> " + Convert.ToString(Convert.ToInt32(TotalVegCover / n )) + " %. </b> "
            + " <br> <img src='images/slope_grade2.png' width='175' /> "
            + "<br><input name='btnKML' type='submit' value='Printer Friendly Output' onclick='ClickToPrint()';>"
            + "<br><br><input name='btnKML' type='submit' value='Download KML' onclick='downloadKML()';>"
            + "<br><br><input name='btnGPS' type='submit' value='Get GPS Coordinates' onclick='showGPSWindow()';>"
            + "<br><HR><B> Suggested Route:</b><HR>"
			+"?"
            + strBuilderPath.ToString().Trim()
            + "?" 
			+ strCoordinates.ToString().Trim();

			//for (m = 0; m < n; m++)
			//{
			//    strBuilderNode.Append(path[m] + " ");
			//}
		

			//while (StartNodeSaved != j)
			//{
			//    if (j == EndNode)
			//    {
			//        strBuilderNode.Append(j + " ");
			//    }
			//    strBuilderNode.Append(Result[1, j] + " ");
			//    j = Result[1, j];
			//}
		}
		//</MN>

    }

}