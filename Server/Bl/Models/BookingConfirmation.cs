namespace Bl.Models
{
    /// <summary>
    /// What a visitor gets back after booking an appointment. It deliberately holds no customer details:
    /// the request is public, so nothing stored about a customer may be echoed back to whoever sent it.
    /// </summary>
    public record BookingConfirmation(int Id, DateOnly AppointmentDate, TimeOnly Hour, int NumberOfPeople);
}
