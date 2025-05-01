using BuzzWatch.Domain.Common;

namespace BuzzWatch.Domain.Measurements
{
    public class MeasurementHeader : BaseEntity<long>
    {
        private MeasurementHeader() { } // EF Core constructor

        public Guid DeviceId { get; private set; }
        public DateTimeOffset RecordedAt { get; private set; }

        public MeasurementTempIn? TempIn { get; private set; }
        public MeasurementHumIn? HumIn { get; private set; }
        public MeasurementTempOut? TempOut { get; private set; }
        public MeasurementHumOut? HumOut { get; private set; }
        public MeasurementWeight? Weight { get; private set; }

        public static MeasurementHeader Create(Guid deviceId, DateTimeOffset at)
        {
            var header = new MeasurementHeader
            {
                DeviceId = deviceId,
                RecordedAt = at
            };
            
            return header;
        }

        public void AttachTempInside(decimal valueC)
        {
            TempIn = new MeasurementTempIn(Id, valueC);
            AddDomainEvent(new MeasurementCreatedEvent(Id, DeviceId));
        }

        public void AttachHumInside(decimal valuePct)
        {
            HumIn = new MeasurementHumIn(Id, valuePct);
        }

        public void AttachTempOutside(decimal valueC)
        {
            TempOut = new MeasurementTempOut(Id, valueC);
        }

        public void AttachHumOutside(decimal valuePct)
        {
            HumOut = new MeasurementHumOut(Id, valuePct);
        }

        public void AttachWeight(decimal valueKg)
        {
            Weight = new MeasurementWeight(Id, valueKg);
        }
    }
} 