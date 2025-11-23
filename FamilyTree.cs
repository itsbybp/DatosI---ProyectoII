using System;
using System.Collections.Generic;
using System.Linq;

namespace WorldMapZoom
{
    public class FamilyTree
    {
        private Dictionary<string, Person> members;
        private Graph graph;

        public FamilyTree()
        {
            members = new Dictionary<string, Person>();
            graph = new Graph();
        }

        public void Clear()
        {
            members.Clear();
            graph = new Graph();
        }

        public void AddPerson(Person person)
        {
            if (!members.ContainsKey(person.Id))
            {
                members[person.Id] = person;
                graph.AddNode(person);
            }
        }

        public void UpdatePerson(Person person)
        {
            if (members.ContainsKey(person.Id))
            {
                members[person.Id] = person;
                graph = new Graph();
                foreach (var p in members.Values)
                {
                    graph.AddNode(p);
                }
            }
        }

        public void RemovePerson(string personId)
        {
            if (!members.ContainsKey(personId))
                return;

            var person = members[personId];

            // Quitar de la lista de hijos de los padres
            foreach (var parentId in person.ParentIds.ToList())
            {
                if (members.ContainsKey(parentId))
                {
                    members[parentId].ChildrenIds.Remove(personId);
                }
            }

            // Remover relación de pareja mutua
            foreach (var spouseId in person.SpouseIds.ToList())
            {
                if (members.ContainsKey(spouseId))
                {
                    members[spouseId].SpouseIds.Remove(personId);
                }
            }

            // Quitar de la lista de padres de los hijos
            foreach (var childId in person.ChildrenIds.ToList())
            {
                if (members.ContainsKey(childId))
                {
                    members[childId].ParentIds.Remove(personId);
                }
            }

            // sacar persona del árbol
            members.Remove(personId);

            // Recalcular distancias
            graph = new Graph();
            foreach (var p in members.Values)
            {
                graph.AddNode(p);
            }
        }

        public void AddParentRelation(string childId, string parentId)
        {
            if (members.ContainsKey(childId) && members.ContainsKey(parentId))
            {
                if (!members[childId].ParentIds.Contains(parentId))
                {
                    members[childId].ParentIds.Add(parentId);
                }
                if (!members[parentId].ChildrenIds.Contains(childId))
                {
                    members[parentId].ChildrenIds.Add(childId);
                }
            }
        }

        public void AddSpouseRelation(string personId1, string personId2)
        {
            if (members.ContainsKey(personId1) && members.ContainsKey(personId2))
            {
                if (!members[personId1].SpouseIds.Contains(personId2))
                {
                    members[personId1].SpouseIds.Add(personId2);
                }
                if (!members[personId2].SpouseIds.Contains(personId1))
                {
                    members[personId2].SpouseIds.Add(personId1);
                }
            }
        }

        public Person GetPerson(string id)
        {
            return members.ContainsKey(id) ? members[id] : null;
        }

        public List<Person> GetAllMembers()
        {
            return members.Values.ToList();
        }

        public List<Person> GetChildren(string personId)
        {
            if (!members.ContainsKey(personId))
                return new List<Person>();

            return members[personId].ChildrenIds
                .Where(id => members.ContainsKey(id))
                .Select(id => members[id])
                .ToList();
        }

        public List<Person> GetParents(string personId)
        {
            if (!members.ContainsKey(personId))
                return new List<Person>();

            return members[personId].ParentIds
                .Where(id => members.ContainsKey(id))
                .Select(id => members[id])
                .ToList();
        }

        public List<Person> GetSpouses(string personId)
        {
            if (!members.ContainsKey(personId))
                return new List<Person>();

            return members[personId].SpouseIds
                .Where(id => members.ContainsKey(id))
                .Select(id => members[id])
                .ToList();
        }

        public Graph GetGraph()
        {
            graph.BuildDistanceGraph();
            return graph;
        }

        public List<Person> GetRootMembers()
        {
            return members.Values
                .Where(p => p.ParentIds.Count == 0)
                .ToList();
        }
    }
}