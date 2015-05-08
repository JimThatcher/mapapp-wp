using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;

namespace mapapp
{
    
    public class quadtree
    {
        static int s_maxpernode = 300;

        public double latmin;
        public double latmax;
        public double longmin;
        public double longmax;

        public quadtree [] children;

        public Stack<PushpinModel> stackModels = new Stack<PushpinModel>();
        
        public void AddNode(PushpinModel model)
        {            
            stackModels.Push(model);
        }

        // returns a list of all of the pushpinmodels in the bounding rectangle
        public List<PushpinModel> Search(double lat1, double lat2, double long1, double long2)
        {
            List<PushpinModel> SearchResults = new List<PushpinModel>();

            SearchInternal(ref SearchResults, lat1, lat2, long1, long2);

            return SearchResults;
        }

        public void SearchInternal(ref List<PushpinModel> results, double lat1, double lat2, double long1, double long2)
        {
            for (int i = 0; i < 4; i++)
            {
                if (null != children && null != children[i])
                {
                    // does the view intersect with the child node?
                    if( lat2 >= children[i].latmin && lat1 <= children[i].latmax &&
                        long2 >= children[i].longmin && long1 <= children[i].longmax)
                    {
                        children[i].SearchInternal(ref results, lat1, lat2, long1, long2);
                    }
                }
            }
            results.AddRange(stackModels);
        }

        public void BuildTree()
        {
            if (stackModels.Count < s_maxpernode)
            {
                // not enough nodes to segment
                return;
            }


            if (null == children)
            {
                double latmid = latmin + ((latmax - latmin) / 2.0);
                double longmid = longmin + ((longmax - longmin) / 2.0);

                children = new quadtree[4];
                children[0] = new quadtree();
                children[0].latmin = latmin;
                children[0].longmin = longmin;       // lat is x, long is y
                children[0].latmax = latmid;         //     
                children[0].longmax = longmid;       //   0  |  1 
                                                     //  ---------
                children[1] = new quadtree();        //   2  |  3
                children[1].latmin = latmid;   
                children[1].longmin = longmin;
                children[1].latmax = latmax;
                children[1].longmax = longmid;

                children[2] = new quadtree();
                children[2].latmin = latmin;
                children[2].latmax = latmid;
                children[2].longmin = longmid;
                children[2].longmax = longmax;

                children[3] = new quadtree();
                children[3].latmin = latmid;
                children[3].latmax = latmax;
                children[3].longmin = longmid;
                children[3].longmax = longmax;
            }

            // push all the children down to leaf nodes

            while (stackModels.Count > 0)
            {
                PushpinModel p = stackModels.Pop();

                for (int i = 0; i < 4; i++)
                {
                    if (p.Location.Latitude >= children[i].latmin &&
                       p.Location.Latitude <= children[i].latmax &&
                       p.Location.Longitude >= children[i].longmin &&
                       p.Location.Longitude <= children[i].longmax)
                    {
                        // child fits into this quadrant
                        children[i].AddNode(p);
                        break;
                    }
                }

            }

            for (int i = 0; i < 4; i++)
            {
                children[i].BuildTree();
            }
        }
    }
}
