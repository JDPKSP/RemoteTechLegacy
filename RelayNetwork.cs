using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RemoteTech
{

    public class RelayNetwork
    {
        public List<RelayNode>
            all,
            comSats,
            commandStations;

        public RelayNetwork()
        {
            all = new List<RelayNode>();
            comSats = new List<RelayNode>();
            commandStations = new List<RelayNode>();


            foreach (Vessel v in FlightGlobals.Vessels)
            {
                if (RTUtils.IsComsat(v)) all.Add(new RelayNode(v));
            }

            foreach (RelayNode node in all)
            {
                if (node.HasCommand) commandStations.Add(node);
                else comSats.Add(node);
            }
            all.Add(new RelayNode());
            commandStations.Add(new RelayNode());

            foreach (KeyValuePair<Vessel, RemoteCore> pair in RTGlobals.coreList)
            {
                try
                {
                    pair.Value.Rnode = new RelayNode(pair.Key);
                }
                catch { }
            }

        }

        public void Reload()
        {
            foreach (RelayNode node in all) node.Reload();
            foreach (RelayNode node in comSats) node.Reload();
            foreach (RelayNode node in commandStations) node.Reload();
        }

        public void Reload(RelayNode reloadNode)
        {
            foreach (RelayNode node in all) if (node.Equals(reloadNode)) node.Reload();

            foreach (RelayNode node in comSats) if (node.Equals(reloadNode)) node.Reload();

            foreach (RelayNode node in commandStations) if (node.Equals(reloadNode)) node.Reload();
        }

        public RelayPath GetCommandPath(RelayNode start)
        {
            double compare = double.MaxValue;
            RelayPath output = null;
            foreach (RelayNode node in commandStations)
            {
                if (!start.Equals(node) && node.HasCommand)
                {
                    RelayPath tmp = findShortestRelayPath(start, node);
                    if (tmp != null && tmp.Length < compare)
                    {
                        output = tmp;
                        compare = tmp.Length;
                    }
                }
            }
            return output;
        }

        public bool inContactWith(RelayNode node, RelayNode other)
        {
            return (findShortestRelayPath(node, other) != null);
        }

        RelayPath findShortestRelayPath(RelayNode start, RelayNode goal)
        {
            HashSet<RelayNode> closedSet = new HashSet<RelayNode>();
            HashSet<RelayNode> openSet = new HashSet<RelayNode>();

            Dictionary<RelayNode, RelayNode> cameFrom = new Dictionary<RelayNode, RelayNode>();
            Dictionary<RelayNode, double> gScore = new Dictionary<RelayNode, double>();
            Dictionary<RelayNode, double> hScore = new Dictionary<RelayNode, double>();
            Dictionary<RelayNode, double> fScore = new Dictionary<RelayNode, double>();

            openSet.Add(start);

            double startBaseHeuristic = (start.Position - goal.Position).magnitude;
            gScore[start] = 0.0;
            hScore[start] = startBaseHeuristic;
            fScore[start] = startBaseHeuristic;


            HashSet<RelayNode> neighbors = new HashSet<RelayNode>(all);
            neighbors.Add(start);
            neighbors.Add(goal);

            RelayPath path = null;
            while (openSet.Count > 0)
            {
                RelayNode current = null;
                double currentBestScore = double.MaxValue;
                foreach (KeyValuePair<RelayNode, double> pair in fScore)
                {
                    if (openSet.Contains(pair.Key) && pair.Value < currentBestScore)
                    {
                        current = pair.Key;
                        currentBestScore = pair.Value;
                    }
                }
                if (current == goal)
                {
                    path = new RelayPath(reconstructPath(cameFrom, goal));
                    break;
                }
                openSet.Remove(current);
                closedSet.Add(current);
                foreach (RelayNode neighbor in neighbors)
                {
                    if (!closedSet.Contains(neighbor) && inRange(neighbor, current) && lineOfSight(neighbor, current))
                    {
                        //double tentGScore = gScore[current] - (neighbor.Position - current.Position).magnitude;
                        double tentGScore = gScore[current] + (neighbor.Position - current.Position).magnitude;

                        bool tentIsBetter = false;
                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                            hScore[neighbor] = (neighbor.Position - goal.Position).magnitude;
                            tentIsBetter = true;
                        }
                        else if (tentGScore < gScore[neighbor])
                        {
                            tentIsBetter = true;
                        }

                        if (tentIsBetter)
                        {
                            cameFrom[neighbor] = current;
                            gScore[neighbor] = tentGScore;
                            fScore[neighbor] = tentGScore + hScore[neighbor];
                        }
                    }
                }

            }

            return path;
        }

        List<RelayNode> reconstructPath(Dictionary<RelayNode, RelayNode> cameFrom, RelayNode curNode)
        {
            List<RelayNode> tmp = null;
            if (cameFrom.ContainsKey(curNode))
            {
                tmp = reconstructPath(cameFrom, cameFrom[curNode]);
                tmp.Add(curNode);
            }
            else
            {
                tmp = new List<RelayNode>() { curNode };
            }
            return tmp;
        }

        // NK pull out distance return
        // NK new distance calcs
        // = weaker + sqrt(weaker * stronger), with these maximums:
        // antenna to either: Max of 100x weaker.
        // dish to dish: Max of 1000x weaker.
        static float distFunc(float minRange, float maxRange, float clamp)
        {
            if (RTGlobals.useNewRange)
            {
                float temp = minRange + (float)Math.Sqrt(minRange * maxRange);
                if (temp > clamp)
                    return clamp;
                return temp;
            }
            else
                return minRange;
        }

        public static float nodeDistance(RelayNode na, RelayNode nb)
        {
            return (float)(na.Position - nb.Position).magnitude;
        }

        public static float maxDistance(RelayNode na, RelayNode nb)
        {
            float aRange = 0, bRange = 0, aSumRange = 0, bSumRange = 0;
            float clamp = 1000f;
            bool aDish = false, bDish = false;
            
            // get max-range dish pointed at other node
            if (na.HasDish)
            {
                foreach (DishData naData in na.DishData)
                {
                    if (((naData.pointedAt.Equals(nb.Orbits) && !na.Orbits.Equals(nb.Orbits)) || naData.pointedAt.Equals(nb.ID)))
                    {
                        aDish = true;
                        if (naData.dishRange >= aRange)
                            aRange = naData.dishRange;
                        aSumRange += naData.dishRange;
                    }
                        
                }
            }
            if(RTGlobals.useMultiple)
                aRange = (float)Math.Round(aRange + (aSumRange - aRange) * 0.25f);

            if(nb.HasDish)
            {
                foreach (DishData nbData in nb.DishData)
                {
                    if (((nbData.pointedAt.Equals(na.Orbits) && !nb.Orbits.Equals(na.Orbits)) || nbData.pointedAt.Equals(na.ID)))
                    {
                        bDish = true;
                        if (nbData.dishRange >= bRange)
                            aRange = nbData.dishRange;
                        bSumRange += nbData.dishRange;
                    }
                }
            }
            if (RTGlobals.useMultiple)
                bRange = (float)Math.Round(bRange + (bSumRange - bRange) * 0.25f);

            // if no dish, get antenna. If neither, fail.
            if (!aDish)
            {
                clamp = 100f;
                if (na.HasAntenna)
                    aRange = na.AntennaRange;
                else
                    return 0f;
            }
            if (!bDish)
            {
                clamp = 100f;
                if (nb.HasAntenna)
                    bRange = nb.AntennaRange;
                else
                    return 0f;
            }

            // return distance using distance function; clamp to 1000x min range if both dishes or 100x if one or both isn't
            if (aRange < bRange)
            {
                return distFunc(aRange, bRange, clamp * aRange);
            }
            else
            {
                return distFunc(bRange, aRange, clamp * bRange);
            }
        }

        bool inRange(RelayNode na, RelayNode nb)
        {

            if (CheatOptions.InfiniteEVAFuel)
                return true;

            // NK refactor distance functions
            float distance = nodeDistance(na, nb) * 0.001f; // convert to km
            return maxDistance(na, nb) >= distance;

        }

        bool lineOfSight(RelayNode na, RelayNode nb)
        {
            if (CheatOptions.InfiniteEVAFuel)
                return true;

            Vector3d a = na.Position;
            Vector3d b = nb.Position;
            foreach (CelestialBody referenceBody in FlightGlobals.Bodies)
            {
                Vector3d bodyFromA = referenceBody.position - a;
                Vector3d bFromA = b - a;
                if (Vector3d.Dot(bodyFromA, bFromA) > 0)
                {
                    Vector3d bFromAnorm = bFromA.normalized;
                    if (Vector3d.Dot(bodyFromA, bFromAnorm) < bFromA.magnitude)
                    { // check lateral offset from line between b and a
                        Vector3d lateralOffset = bodyFromA - Vector3d.Dot(bodyFromA, bFromAnorm) * bFromAnorm;
                        if (lateralOffset.magnitude < (referenceBody.Radius - 5))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }


        void print(String s)
        {
            MonoBehaviour.print(s);
        }
    }
}
