using System;
using System.Collections.Generic;
using System.Linq;

namespace WorldMapZoom
{
    public class Graph
    {
        private Dictionary<string, List<Edge>> adjacencyList;
        private Dictionary<string, Person> nodes;

        public Graph()
        {
            adjacencyList = new Dictionary<string, List<Edge>>();
            nodes = new Dictionary<string, Person>();
        }

        public void AddNode(Person person)
        {
            if (!nodes.ContainsKey(person.Id))
            {
                nodes[person.Id] = person;
                adjacencyList[person.Id] = new List<Edge>();
            }
        }

        public void AddEdge(string fromId, string toId, double weight)
        {
            if (adjacencyList.ContainsKey(fromId))
            {
                adjacencyList[fromId].Add(new Edge(toId, weight));
            }
        }

        public void BuildDistanceGraph()
        {
            // Construir el grafo completo con todas las distancias
            var personList = nodes.Values.ToList();
            
            foreach (var person1 in personList)
            {
                foreach (var person2 in personList)
                {
                    if (person1.Id != person2.Id)
                    {
                        double distance = DistanceCalculator.CalculateDistance(
                            person1.Latitude, person1.Longitude,
                            person2.Latitude, person2.Longitude);
                        
                        AddEdge(person1.Id, person2.Id, distance);
                    }
                }
            }
        }

        public Dictionary<string, double> GetDistancesFrom(string personId)
        {
            var distances = new Dictionary<string, double>();
            
            if (!adjacencyList.ContainsKey(personId))
                return distances;

            foreach (var edge in adjacencyList[personId])
            {
                distances[edge.ToId] = edge.Weight;
            }

            return distances;
        }

        public Person GetPerson(string id)
        {
            return nodes.ContainsKey(id) ? nodes[id] : null;
        }

        public List<Person> GetAllPersons()
        {
            return nodes.Values.ToList();
        }

        public (Person, Person, double) GetFarthestPair()
        {
            double maxDistance = 0;
            Person person1 = null;
            Person person2 = null;

            var personList = nodes.Values.ToList();
            
            for (int i = 0; i < personList.Count; i++)
            {
                for (int j = i + 1; j < personList.Count; j++)
                {
                    double distance = DistanceCalculator.CalculateDistance(
                        personList[i].Latitude, personList[i].Longitude,
                        personList[j].Latitude, personList[j].Longitude);

                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                        person1 = personList[i];
                        person2 = personList[j];
                    }
                }
            }

            return (person1, person2, maxDistance);
        }

        public (Person, Person, double) GetClosestPair()
        {
            double minDistance = double.MaxValue;
            Person person1 = null;
            Person person2 = null;

            var personList = nodes.Values.ToList();

            for (int i = 0; i < personList.Count; i++)
            {
                for (int j = i + 1; j < personList.Count; j++)
                {
                    double distance = DistanceCalculator.CalculateDistance(
                        personList[i].Latitude, personList[i].Longitude,
                        personList[j].Latitude, personList[j].Longitude);

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        person1 = personList[i];
                        person2 = personList[j];
                    }
                }
            }

            return (person1, person2, minDistance);
        }

        public double GetAverageDistance()
        {
            var personList = nodes.Values.ToList();
            if (personList.Count < 2) return 0;

            double totalDistance = 0;
            int count = 0;

            for (int i = 0; i < personList.Count; i++)
            {
                for (int j = i + 1; j < personList.Count; j++)
                {
                    double distance = DistanceCalculator.CalculateDistance(
                        personList[i].Latitude, personList[i].Longitude,
                        personList[j].Latitude, personList[j].Longitude);

                    totalDistance += distance;
                    count++;
                }
            }

            return count > 0 ? totalDistance / count : 0;
        }
    }

    public class Edge
    {
        public string ToId { get; set; }
        public double Weight { get; set; }

        public Edge(string toId, double weight)
        {
            ToId = toId;
            Weight = weight;
        }
    }
}