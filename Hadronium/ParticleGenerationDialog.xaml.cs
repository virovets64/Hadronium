using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Hadronium
{
    /// <summary>
    /// Interaction logic for ParticleGenerationDialog.xaml
    /// </summary>
    public partial class ParticleGenerationDialog : Window
    {
        public ParticleGenerationDialog()
        {
            InitializeComponent();
        }

        public int ParticleCount
        {
            get { return int.Parse(textBoxParticles.Text); }
            set { textBoxParticles.Text = value.ToString(); }
        }

        public int LinkCount
        {
            get { return int.Parse(textBoxLinks.Text); }
            set { textBoxLinks.Text = value.ToString(); }
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            int maxLinkCount = ParticleCount * (ParticleCount - 1) / 2;
            if (LinkCount > maxLinkCount)
                MessageBox.Show(this, "Error", string.Format("{0} particles cannot have more than {1} links", ParticleCount, maxLinkCount));
            else
                this.DialogResult = true;
        }
    }
}
