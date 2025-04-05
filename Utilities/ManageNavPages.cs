using Microsoft.AspNetCore.Mvc.Rendering;

namespace Secure_Student_Management_System.Utilities
{
    public class ManageNavPages
    {
        public static string? IndexClass(ViewContext viewContext)
             => PageActiveClass(viewContext, "Index");

        public static string? SecurityClass(ViewContext viewContext)
            => PageActiveClass(viewContext, "ChangePassword");

        public static string? ConnectionsClass(ViewContext viewContext)
            => PageActiveClass(viewContext, "ManageLogins");

        private static string? PageActiveClass(ViewContext viewContext, string page)
        {
            var activePage = viewContext.ViewData["ActivePage"] as string
                ?? Path.GetFileNameWithoutExtension(viewContext.ActionDescriptor.DisplayName);

            return string.Equals(activePage, page, StringComparison.OrdinalIgnoreCase)
                ? "active"
                : null;
        }
    }
}
