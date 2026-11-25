using System;
using System.Text.Encodings.Web;

namespace Onion.Common.Helpers
{
    public static class WhatsAppAgendaTemplates
    {
        public static string BuildDirectWhatsAppUrl(string phoneNumber, string message)
        {
            var cleanPhone = (phoneNumber ?? "").Replace("+", "").Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Trim();
            var encodedMsg = UrlEncoder.Default.Encode(message ?? "");
            return $"https://wa.me/{cleanPhone}?text={encodedMsg}";
        }

        public static string BuildConfirmationMessage(string businessName, string customerName, string bookingCode, string serviceName, DateTime startAt, string statusUrl)
        {
            return $"¡Hola {customerName}! 👋 Tu cita en *{businessName}* ha sido registrada con éxito.\n\n" +
                   $"✂️ *Servicio:* {serviceName}\n" +
                   $"📅 *Fecha:* {startAt:dd/MM/yyyy}\n" +
                   $"⏰ *Hora:* {startAt:hh:mm tt}\n" +
                   $"🔖 *Código de cita:* {bookingCode}\n\n" +
                   $"Puedes consultar o gestionar tu cita en cualquier momento aquí:\n{statusUrl}\n\n" +
                   $"¡Gracias por elegirnos!";
        }

        public static string BuildReminderMessage(string businessName, string customerName, string serviceName, DateTime startAt)
        {
            return $"¡Hola {customerName}! ⏰ Te recordamos tu cita de *{serviceName}* en *{businessName}* programada para hoy a las *{startAt:hh:mm tt}*.\n\n" +
                   $"Te esperamos puntualmente. ¡Que tengas un excelente día!";
        }
    }
}
