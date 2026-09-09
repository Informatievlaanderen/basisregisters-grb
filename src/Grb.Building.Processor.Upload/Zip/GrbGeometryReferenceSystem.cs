namespace Grb.Building.Processor.Upload.Zip
{
    using Be.Vlaanderen.Basisregisters.GrAr.Common.NetTopology;
    using Be.Vlaanderen.Basisregisters.GrAr.CrsTransform;
    using NetTopologySuite.Geometries;

    /// <summary>
    /// Which reference system a GRB delivery is in, decided by where its coordinates fall.
    /// </summary>
    /// <remarks>
    /// Nothing in the delivery says so. The archive holds only the shape, dbase and index files - no
    /// <c>.prj</c> - and the uploaded filename does not survive: the pre-signed upload writes to the fixed
    /// key <c>upload_{jobId}</c>, so what the operator called the file never reaches this code.
    ///
    /// The coordinates are unambiguous, which is what makes deciding from them safe here. The two Flanders
    /// envelopes do not overlap on either axis - Lambert 72 spans x 21 492..259 366, Lambert 2008 spans
    /// x 521 399..759 275 - so a polygon can fall in at most one of them, and a real building cannot be
    /// mistaken for the other system. Parcel-registry decides the same question the same way, and for the
    /// same reason: GRB geometry that arrives carrying no trustworthy label. See ADR 0003.
    /// </remarks>
    public static class GrbGeometryReferenceSystem
    {
        /// <returns>
        /// The SRID the coordinates are in, or <c>null</c> when they fall in neither envelope - which
        /// cannot be resolved and so must be refused rather than guessed at.
        /// </returns>
        public static int? Of(Geometry geometry)
        {
            if (geometry.IsInsideFlandersUsingLambert72())
            {
                return SystemReferenceId.SridLambert72;
            }

            return geometry.IsInsideFlandersUsingLambert08()
                ? SystemReferenceId.SridLambert2008
                : null;
        }
    }
}
