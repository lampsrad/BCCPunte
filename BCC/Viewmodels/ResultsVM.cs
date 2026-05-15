using BCC.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BCC.Viewmodels
{
    public class ResultsVM
    {
        public IList<Photo> N1 = new List<Photo>();
        public IList<Photo> P1 = new List<Photo>();
        public IList<Photo> N2 = new List<Photo>();
        public IList<Photo> P2 = new List<Photo>();
        public IList<Photo> N3 = new List<Photo>();
        public IList<Photo> P3 = new List<Photo>();
        public IList<Photo> N4 = new List<Photo>();
        public IList<Photo> P4 = new List<Photo>();
        public IList<Photo> S = new List<Photo>();
        public IList<Photo> PH = new List<Photo>(); 
        public IList<Photo> Winners = new List<Photo>();
    }
}
