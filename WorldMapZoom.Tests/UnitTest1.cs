using NUnit.Framework;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WorldMapZoom;

using Assert = NUnit.Framework.Legacy.ClassicAssert;

namespace WorldMapZoom.Tests
{
    [TestFixture]
    [Apartment(System.Threading.ApartmentState.STA)] // Requerido para Windows Forms
    public class StatisticsFormTests
    {
        private FamilyTree _familyTree;
        private Person _person1;
        private Person _person2;
        private Person _person3;

        [SetUp]
        public void SetUp()
        {
            _familyTree = new FamilyTree();

            // Crear personas de prueba con diferentes ubicaciones
            _person1 = new Person
            {
                Id = "p1",
                FirstName = "Juan",
                FirstLastName = "Pérez",
                NationalId = "123456789",
                BirthDate = new DateTime(1980, 1, 1),
                IsAlive = true,
                Age = 44,
                PhotoUrl = "test.jpg",
                Latitude = 9.9281, // San José, Costa Rica
                Longitude = -84.0907,
                Address = "San José, Costa Rica"
            };

            _person2 = new Person
            {
                Id = "p2",
                FirstName = "María",
                FirstLastName = "González",
                NationalId = "987654321",
                BirthDate = new DateTime(1985, 5, 15),
                IsAlive = true,
                Age = 39,
                PhotoUrl = "test2.jpg",
                Latitude = 10.6318, // Liberia, Costa Rica (~100km de San José)
                Longitude = -85.4376,
                Address = "Liberia, Guanacaste"
            };

            _person3 = new Person
            {
                Id = "p3",
                FirstName = "Carlos",
                FirstLastName = "Rodríguez",
                NationalId = "456789123",
                BirthDate = new DateTime(1990, 12, 25),
                IsAlive = true,
                Age = 34,
                PhotoUrl = "test3.jpg",
                Latitude = 9.9936, // Cartago, Costa Rica (~20km de San José)
                Longitude = -84.0083,
                Address = "Cartago, Costa Rica"
            };
        }

        [TearDown]
        public void TearDown()
        {
            _familyTree?.Clear();
        }

        #region Constructor Tests

        [Test]
        public void Constructor_WithValidFamilyTree_CreatesForm()
        {
            // Arrange & Act
            using (var form = new StatisticsForm(_familyTree))
            {
                // Assert
                Assert.IsNotNull(form);
                Assert.AreEqual("Estadísticas del Árbol Genealógico", form.Text);
                Assert.AreEqual(700, form.Width);
                Assert.AreEqual(550, form.Height);
                Assert.AreEqual(FormStartPosition.CenterParent, form.StartPosition);
                Assert.AreEqual(Color.White, form.BackColor);
            }
        }
        [Test]
        public void Constructor_WithEmptyFamilyTree_ShowsWarningMessage()
        {
            // Arrange
            var emptyTree = new FamilyTree();

            // Act
            using (var form = new StatisticsForm(emptyTree))
            {
                // Assert
                Assert.IsNotNull(form, "El formulario debe crearse con árbol vacío");
                Assert.AreEqual("Estadísticas del Árbol Genealógico", form.Text);
                Assert.AreEqual(700, form.Width);
                Assert.AreEqual(550, form.Height);

                // Verificar que muestra mensaje de advertencia
                var labels = GetAllLabels(form);
                var hasWarning = labels.Any(l =>
                    l.Text.Contains("2 personas") ||
                    l.Text.Contains("al menos") ||
                    l.Text.Contains("necesitan"));

                Assert.IsTrue(hasWarning, "Debe mostrar advertencia cuando no hay suficientes personas");
            }
        }

        [Test]
        public void CalculateStatistics_WithLessThanTwoPeople_ShowsWarningMessage()
        {
            // Arrange
            _familyTree.AddPerson(_person1);

            // Act
            using (var form = new StatisticsForm(_familyTree))
            {
                // Assert
                var labels = GetAllLabels(form);
                var warningLabel = labels.FirstOrDefault(l =>
                    l.Text.Contains("Se necesitan al menos 2 personas") ||
                    l.Text.Contains("2 personas") ||
                    l.Text.Contains("al menos"));

                Assert.IsNotNull(warningLabel, "Debería mostrar mensaje de advertencia con solo 1 persona");
                Assert.AreEqual(Color.DarkOrange, warningLabel.ForeColor);
            }
        }

        [Test]
        public void CalculateStatistics_WithTwoPeople_ShowsCorrectDistances()
        {
            // Arrange
            _familyTree.AddPerson(_person1);
            _familyTree.AddPerson(_person2);

            // Act
            using (var form = new StatisticsForm(_familyTree))
            {
                var graph = _familyTree.GetGraph();
                var (farthest1, farthest2, maxDist) = graph.GetFarthestPair();
                var (closest1, closest2, minDist) = graph.GetClosestPair();
                var avgDist = graph.GetAverageDistance();

                // Assert
                Assert.IsNotNull(farthest1, "Debe encontrar el par más lejano");
                Assert.IsNotNull(farthest2, "Debe encontrar el par más lejano");
                Assert.Greater(maxDist, 0, "La distancia máxima debe ser mayor a 0");

                Assert.IsNotNull(closest1, "Debe encontrar el par más cercano");
                Assert.IsNotNull(closest2, "Debe encontrar el par más cercano");
                Assert.Greater(minDist, 0, "La distancia mínima debe ser mayor a 0");

                Assert.AreEqual(maxDist, minDist, 0.01,
                    "Con 2 personas, la distancia máxima y mínima deben ser iguales");
                Assert.AreEqual(avgDist, maxDist, 0.01,
                    "Con 2 personas, el promedio debe ser igual a la distancia entre ellas");
            }
        }

        [Test]
        public void CalculateStatistics_WithThreePeople_CalculatesCorrectly()
        {
            // Arrange
            _familyTree.AddPerson(_person1); // San José
            _familyTree.AddPerson(_person2); // Liberia (~100km)
            _familyTree.AddPerson(_person3); // Cartago (~20km)

            // Act
            using (var form = new StatisticsForm(_familyTree))
            {
                var graph = _familyTree.GetGraph();
                var (farthest1, farthest2, maxDist) = graph.GetFarthestPair();
                var (closest1, closest2, minDist) = graph.GetClosestPair();
                var avgDist = graph.GetAverageDistance();

                // Assert - Verificaciones básicas
                Assert.Greater(maxDist, minDist, "La distancia máxima debe ser mayor que la mínima");
                Assert.Greater(maxDist, 0, "La distancia máxima debe ser positiva");
                Assert.Greater(minDist, 0, "La distancia mínima debe ser positiva");
                Assert.Greater(avgDist, 0, "El promedio debe ser positivo");

                // Verificar rangos razonables
                Assert.Greater(maxDist, 80, "La distancia máxima debe ser considerable");
                Assert.Less(minDist, 40, "La distancia mínima debe ser pequeña");
            }
        }

        #endregion

        #region UI Components Tests

        [Test]
        public void InitializeUI_CreatesMainPanel()
        {
            // Arrange & Act
            using (var form = new StatisticsForm(_familyTree))
            {
                // Assert
                var mainPanel = form.Controls.OfType<Panel>().FirstOrDefault();
                Assert.IsNotNull(mainPanel, "Debe existir un panel principal");
                Assert.AreEqual(DockStyle.Fill, mainPanel.Dock);
                Assert.IsTrue(mainPanel.AutoScroll);
                Assert.AreEqual(Color.White, mainPanel.BackColor);
            }
        }

        [Test]
        public void InitializeUI_CreatesTitleLabel()
        {
            // Arrange & Act
            using (var form = new StatisticsForm(_familyTree))
            {
                // Assert
                var mainPanel = form.Controls.OfType<Panel>().FirstOrDefault();
                var titleLabel = mainPanel?.Controls.OfType<Label>()
                    .FirstOrDefault(l => l.Text.Contains("ESTADÍSTICAS"));

                Assert.IsNotNull(titleLabel, "Debe existir un label de título");
                Assert.AreEqual("ESTADÍSTICAS DE DISTANCIAS", titleLabel.Text);
                Assert.AreEqual(Color.FromArgb(0, 122, 204), titleLabel.ForeColor);
                Assert.Greater(titleLabel.Font.Size, 12, "El título debe tener fuente grande");
            }
        }

        [Test]
        public void CalculateStatistics_WithMultiplePeople_DisplaysAllSections()
        {
            // Arrange
            _familyTree.AddPerson(_person1);
            _familyTree.AddPerson(_person2);
            _familyTree.AddPerson(_person3);

            // Act
            using (var form = new StatisticsForm(_familyTree))
            {
                // Assert
                var mainPanel = form.Controls.OfType<Panel>().FirstOrDefault();
                var labels = mainPanel.Controls.OfType<Label>().ToList();

                // Verificar que existen las 3 secciones principales
                var farthestLabel = labels.FirstOrDefault(l =>
                    l.Text.Contains("FAMILIARES MÁS DISTANTES"));
                var closestLabel = labels.FirstOrDefault(l =>
                    l.Text.Contains("FAMILIARES MÁS CERCANOS"));
                var avgLabel = labels.FirstOrDefault(l =>
                    l.Text.Contains("DISTANCIA PROMEDIO"));

                Assert.IsNotNull(farthestLabel, "Debe mostrar sección de más distantes");
                Assert.IsNotNull(closestLabel, "Debe mostrar sección de más cercanos");
                Assert.IsNotNull(avgLabel, "Debe mostrar sección de promedio");

                // Verificar colores correctos
                Assert.AreEqual(Color.DarkRed, farthestLabel.ForeColor);
                Assert.AreEqual(Color.DarkGreen, closestLabel.ForeColor);
                Assert.AreEqual(Color.DarkBlue, avgLabel.ForeColor);
            }
        }

        [Test]
        public void CalculateStatistics_DisplaysDistanceInformation()
        {
            // Arrange
            _familyTree.AddPerson(_person1);
            _familyTree.AddPerson(_person2);

            // Act
            using (var form = new StatisticsForm(_familyTree))
            {
                // Assert
                var mainPanel = form.Controls.OfType<Panel>().FirstOrDefault();
                Assert.IsNotNull(mainPanel, "Debe existir un panel principal");

                var dataPanels = mainPanel.Controls.OfType<Panel>()
                    .Where(p => p.BackColor == Color.FromArgb(240, 240, 240))
                    .ToList();

                Assert.Greater(dataPanels.Count, 0, "Debe haber al menos un panel de datos con información");

                // Verificar que al menos un panel contiene texto con "km"
                bool hasDistanceInfo = false;
                foreach (var panel in dataPanels)
                {
                    var labels = panel.Controls.OfType<Label>().ToList();
                    if (labels.Any(l => l.Text.Contains("km")))
                    {
                        hasDistanceInfo = true;
                        break;
                    }
                }

                Assert.IsTrue(hasDistanceInfo, "Debe mostrar información de distancia en kilómetros");
            }
        }

        #endregion

        #region Integration Tests

        [Test]
        public void StatisticsForm_IntegrationTest_CompleteFlow()
        {
            // Arrange - Crear un árbol genealógico completo
            _familyTree.AddPerson(_person1);
            _familyTree.AddPerson(_person2);
            _familyTree.AddPerson(_person3);

            // Act
            using (var form = new StatisticsForm(_familyTree))
            {
                form.Show();
                Application.DoEvents(); // Procesar eventos pendientes

                // Assert - Verificar que el formulario está completamente funcional
                Assert.IsTrue(form.Visible);
                Assert.IsFalse(form.IsDisposed);

                var graph = _familyTree.GetGraph();
                var (farthest1, farthest2, maxDist) = graph.GetFarthestPair();
                var (closest1, closest2, minDist) = graph.GetClosestPair();
                var avgDist = graph.GetAverageDistance();

                // Verificar que las estadísticas tienen sentido
                Assert.Greater(maxDist, minDist, "La distancia máxima debe ser mayor que la mínima");
                Assert.Greater(avgDist, 0, "El promedio debe ser positivo");
                Assert.LessOrEqual(avgDist, maxDist, "El promedio no puede ser mayor que el máximo");
                Assert.GreaterOrEqual(avgDist, minDist, "El promedio no puede ser menor que el mínimo");

                form.Close();
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Obtiene todos los labels de un formulario recursivamente
        /// </summary>
        private System.Collections.Generic.List<Label> GetAllLabels(Form form)
        {
            var labels = new System.Collections.Generic.List<Label>();
            GetLabelsRecursive(form, labels);
            return labels;
        }

        private void GetLabelsRecursive(Control parent, System.Collections.Generic.List<Label> labels)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is Label label)
                {
                    labels.Add(label);
                }
                if (control.HasChildren)
                {
                    GetLabelsRecursive(control, labels);
                }
            }
        }

        #endregion
    }
}