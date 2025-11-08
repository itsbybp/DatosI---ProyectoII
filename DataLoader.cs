using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Globalization;

namespace WorldMapZoom
{
    public static class DataLoader
    {
        private static string _currentFilePath;

        public static string GetDefaultJsonPath()
        {
            // Usar el directorio de trabajo actual (configurado como StartWorkingDirectory)
            string workingDir = Environment.CurrentDirectory;
            string jsonPath = Path.Combine(workingDir, "usuarios.json");
            
            if (File.Exists(jsonPath))
            {
                return jsonPath;
            }
            
            // Fallback: buscar desde el directorio base si no se encuentra en working directory
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            DirectoryInfo current = new DirectoryInfo(baseDir);
            while (current != null && current.Parent != null)
            {
                string fallbackPath = Path.Combine(current.FullName, "usuarios.json");
                if (File.Exists(fallbackPath))
                {
                    return fallbackPath;
                }
                current = current.Parent;
            }
            
            // Último fallback: crear en el directorio de trabajo
            return jsonPath;
        }

        public static void SetCurrentFilePath(string path)
        {
            _currentFilePath = path;
        }

        public static void LoadFromJson(string filePath, FamilyTree familyTree)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("El archivo no existe", filePath);

            _currentFilePath = filePath;
            string json = File.ReadAllText(filePath, Encoding.UTF8);
            var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!root.TryGetProperty("people", out var peopleArray))
                throw new Exception("El JSON no contiene la propiedad 'people'");

            // Limpiar árbol actual
            familyTree.Clear();

            // Primera pasada: crear todas las personas
            foreach (var personElement in peopleArray.EnumerateArray())
            {
                var person = ParsePerson(personElement);
                familyTree.AddPerson(person);
            }

            // Segunda pasada: crear las relaciones
            foreach (var personElement in peopleArray.EnumerateArray())
            {
                string personId = personElement.GetProperty("id").GetString();
                
                if (personElement.TryGetProperty("relations", out var relations))
                {
                    if (relations.TryGetProperty("parents", out var parents))
                    {
                        foreach (var parentId in parents.EnumerateArray())
                        {
                            string pid = parentId.GetString();
                            familyTree.AddParentRelation(personId, pid);
                        }
                    }

                    if (relations.TryGetProperty("spouses", out var spouses))
                    {
                        foreach (var spouseId in spouses.EnumerateArray())
                        {
                            string sid = spouseId.GetString();
                            familyTree.AddSpouseRelation(personId, sid);
                        }
                    }
                }
            }
        }

        private static Person ParsePerson(JsonElement element)
        {
            string id = element.GetProperty("id").GetString();
            
            // Parsear nombre completo
            string fullName = element.GetProperty("name").GetString();
            var nameParts = fullName.Split(' ');
            
            string nationalId = element.GetProperty("national_id").GetString();
            string birthDateStr = element.GetProperty("birth_date").GetString();
            bool isAlive = element.GetProperty("alive").GetBoolean();
            int age = element.GetProperty("age").GetInt32();
            string photoUrl = element.GetProperty("photo_url").GetString();

            var location = element.GetProperty("location");
            double lat = location.GetProperty("lat").GetDouble();
            double lng = location.GetProperty("lng").GetDouble();
            string address = location.GetProperty("address").GetString();

            DateTime birthDate = DateTime.Parse(birthDateStr);
            DateTime? deathDate = null;

            if (element.TryGetProperty("death_date", out var deathDateElement))
            {
                string deathDateStr = deathDateElement.GetString();
                if (!string.IsNullOrEmpty(deathDateStr))
                    deathDate = DateTime.Parse(deathDateStr);
            }

            var person = new Person
            {
                Id = id,
                FirstName = nameParts.Length > 0 ? nameParts[0] : "",
                SecondName = nameParts.Length > 1 ? nameParts[1] : "",
                FirstLastName = nameParts.Length > 2 ? nameParts[2] : "",
                SecondLastName = nameParts.Length > 3 ? nameParts[3] : "",
                NationalId = nationalId,
                BirthDate = birthDate,
                IsAlive = isAlive,
                DeathDate = deathDate,
                Age = age,
                PhotoUrl = photoUrl,
                Latitude = lat,
                Longitude = lng,
                Address = address
            };

            return person;
        }

        public static void SaveToJson(FamilyTree familyTree)
        {
            string filePath = _currentFilePath ?? GetDefaultJsonPath();
            SaveToJson(familyTree, filePath);
        }

        public static void SaveToJson(FamilyTree familyTree, string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"people\": [");

            var members = familyTree.GetAllMembers();
            for (int i = 0; i < members.Count; i++)
            {
                var person = members[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"id\": \"{person.Id}\",");
                sb.AppendLine($"      \"name\": \"{person.FullName}\",");
                sb.AppendLine($"      \"national_id\": \"{person.NationalId}\",");
                sb.AppendLine($"      \"birth_date\": \"{person.BirthDate:yyyy-MM-dd}\",");
                
                if (!person.IsAlive && person.DeathDate.HasValue)
                {
                    sb.AppendLine($"      \"death_date\": \"{person.DeathDate.Value:yyyy-MM-dd}\",");
                }
                
                sb.AppendLine($"      \"alive\": {person.IsAlive.ToString().ToLower()},");
                sb.AppendLine($"      \"age\": {person.Age},");
                sb.AppendLine($"      \"photo_url\": \"{person.PhotoUrl}\",");
                sb.AppendLine("      \"location\": {");
                sb.AppendLine($"        \"lat\": {person.Latitude.ToString(CultureInfo.InvariantCulture)},");
                sb.AppendLine($"        \"lng\": {person.Longitude.ToString(CultureInfo.InvariantCulture)},");
                sb.AppendLine($"        \"address\": \"{person.Address}\"");
                sb.AppendLine("      },");
                sb.AppendLine("      \"relations\": {");
                sb.Append($"        \"parents\": [{string.Join(", ", person.ParentIds.Select(id => $"\"{id}\""))}],");
                sb.AppendLine();
                sb.Append($"        \"spouses\": [{string.Join(", ", person.SpouseIds.Select(id => $"\"{id}\""))}],");
                sb.AppendLine();
                sb.Append($"        \"children\": [{string.Join(", ", person.ChildrenIds.Select(id => $"\"{id}\""))}]");
                sb.AppendLine();
                sb.AppendLine("      }");
                
                if (i < members.Count - 1)
                    sb.AppendLine("    },");
                else
                    sb.AppendLine("    }");
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            _currentFilePath = filePath;
        }
    }
}