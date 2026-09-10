namespace Grb.Building.Processor.Upload.Zip.Validators
{
    public enum ValidationErrorType
    {
        GeometryIsNotPolygon,
        PolygonNotValid,
        UnknownEventType,
        InvalidGrId,
        InvalidVersionDate,
        InvalidEndDate,
        DuplicateNewBuilding,

        /// <summary>The coordinates fall in neither the Lambert 72 nor the Lambert 2008 Flanders envelope,
        /// so the reference system they are in cannot be established. See ADR 0003.</summary>
        GeometryOutsideFlanders,

        /// <summary>The archive holds records in both reference systems. A GRB delivery is in one or the
        /// other. See ADR 0003.</summary>
        MixedReferenceSystems,
    }
}
