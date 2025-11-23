using System;
using System.Collections.Generic;
using System.Linq;

namespace WorldMapZoom
{
    // Grafo implementado desde cero
    // Almacena información de residencia de cada persona y calcula distancias geográficas
    public class Graph
    {
        // lista de adyacencia: cada persona tiene una lista de aristas hacia otras personas
        private Dictionary<string, List<Edge>> adjacencyList;
        
        // Diccionario para guardar los nodos (personas) del grafo
        private Dictionary<string, Person> nodes;

        public Graph()
        {
            adjacencyList = new Dictionary<string, List<Edge>>();
            nodes = new Dictionary<string, Person>();
        }

        // Agregar un nodo al grafo (una persona con su info de residencia)
        public void AddNode(Person person)
        {
            if (!nodes.ContainsKey(person.Id))
            {
                nodes[person.Id] = person;
                adjacencyList[person.Id] = new List<Edge>();
            }
        }

        // Agregar arista entre dos personas con un peso (distancia en km)
        public void AddEdge(string fromId, string toId, double weight)
        {
            if (adjacencyList.ContainsKey(fromId))
            {
                adjacencyList[fromId].Add(new Edge(toId, weight));
            }
        }

        // Construir el grafo completo calculando todas las distancias
        // Esto crea un grafo dirigido y ponderado donde cada arista tiene el peso de la distancia real
        public void BuildDistanceGraph()
        {
            var personList = nodes.Values.ToList();
            
            // Recorrer todos los pares de personas
            foreach (var person1 in personList)
            {
                foreach (var person2 in personList)
                {
                    if (person1.Id != person2.Id)
                    {
                        // Calcular distancia geografica usando Haversine
                        // Esto usa lat/long que es la info de residencia de cada nodo
                        double distance = DistanceCalculator.CalculateDistance(
                            person1.Latitude, person1.Longitude,
                            person2.Latitude, person2.Longitude);
                        
                        // Crear arista con peso = distancia
                        AddEdge(person1.Id, person2.Id, distance);
                    }
                }
            }
        }

        // Obtener todas las distancias desde una persona específica
        // Usado para mostrar las líneas en el mapa cuando se hace click
        public Dictionary<string, double> GetDistancesFrom(string personId)
        {
            var distances = new Dictionary<string, double>();
            
            if (!adjacencyList.ContainsKey(personId))
                return distances;

            // Recorrer las aristas del nodo y devolver los pesos
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

        // Encontrar el par de personas más lejanas geográficamente
        // Lo que hace es que recorre todos los pares posibles y guarda el máximo
        public (Person, Person, double) GetFarthestPair()
        {
            double maxDistance = 0;
            Person person1 = null;
            Person person2 = null;

            var personList = nodes.Values.ToList();
            
            // Comparar cada par solo una vez (i < j)
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

        // Encontrar el par de personas mas cercanas geograficamente
        // Mismo algoritmo que GetFarthestPair pero buscando el mínimo
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

        // Calcular la distancia promedio entre todos los miembros de la familia
        // útil para ver qué tan dispersa está la familia geográficamente
        public double GetAverageDistance()
        {
            var personList = nodes.Values.ToList();
            if (personList.Count < 2) return 0;

            double totalDistance = 0;
            int count = 0;

            // Sumar todas las distancias
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

    // Clase Edge (arista) implementada desde cero
    // representa una conexión entre dos personas con un peso (distancia)
    public class Edge
    {
        public string ToId { get; set; }      // id de la persona destino
        public double Weight { get; set; }    // peso = distancia en kilómetros

        public Edge(string toId, double weight)
        {
            ToId = toId;
            Weight = weight;
        }
    }
}