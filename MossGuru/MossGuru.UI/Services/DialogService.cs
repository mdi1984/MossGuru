using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MossGuru.UI.Services
{
  public class DialogService
  {
    public string OpenFolderDialog()
    {
      var dialog = new FolderBrowserDialog();
      return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : null;
    }
  }
}
