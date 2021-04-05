using System;
using System.Collections;
using System.Collections.Generic;

namespace ALE_GpsRoulette.ALE_GpsRoulette {

    public class PurchaseCollection : IEnumerable<PurchaseMode> {

        private readonly HashSet<PurchaseMode> modes;

        public int Count { get { return modes.Count; } }

        public PurchaseCollection() {
            modes = new HashSet<PurchaseMode>();
        }

        public void Add(PurchaseMode mode) {
            modes.Add(mode);
        }

        public IEnumerator<PurchaseMode> GetEnumerator() {
            return modes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return modes.GetEnumerator();
        }
    }
}
