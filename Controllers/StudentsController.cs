using DAL;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static Controllers.AccessControl;

namespace Controllers
{
    [UserAccess(Access.View)]
    public class StudentsController : Controller
    {
        public ActionResult Index()
        {
            var years = DB.Students.ToList().Select(s => s.Year).Distinct().ToList();
            Session["StudentsYearsList"] = years;
            Session["SearchYear"] = 0; // reset du filtre à chaque retour sur la liste
            return View();
        }

        public ActionResult GetStudents(bool forceRefresh = false)
        {
            List<Student> result = DB.Students.ToList();

            if (Session["Search"] != null && (bool)Session["Search"])
            {
                if (Session["SearchString"] == null)
                    Session["SearchString"] = "";
                if (Session["SearchYear"] == null)
                    Session["SearchYear"] = 0;
                result = result.Where(c => c.FullName.ToLower().Contains((string)Session["SearchString"])).ToList();

                if((int)Session["SearchYear"] !=0)
                    result = result.Where(c => c.Year == (int)Session["SearchYear"]).ToList();

            }

            List<int> years = new List<int>();
            foreach (Student student in result.ToList())
            {
                if (!years.Contains(student.Year))
                    years.Add(student.Year);
            }
            Session["StudentsYearsList"] = years;
            if (DB.Students.HasChanged || forceRefresh || (Session["Search"] != null && (bool)Session["Search"]))
            {
               

               
                return PartialView(result);
            }

           
            return Content("");

        }


        public ActionResult Details(int id)
        {
            Session["CurrentStudentId"] = id;
            Session["UserCanEditCurrentStudent"] = false;
            Student student = DB.Students.Get(id);

            if(student != null)
            {
                Session["UserCanEditCurrentStudent"] = Models.User.ConnectedUser.IsAdmin || Models.User.ConnectedUser.CanWrite;
                return View(student);
            }
            return RedirectToAction("Index");
        }

        public ActionResult GetDetails(bool forceRefresh = false)
        {
            if (Session["CurrentStudentId"] == null)
                Session["CurrentStudentId"] = 0;

            int studentId = (int)Session["CurrentStudentId"];
            Student student = DB.Students.Get(studentId);
            if(DB.Students.IsMarkedChanged || forceRefresh)
            {
                return PartialView(student);
            }
            return Content("");
        }


        public ActionResult GetInscriptions(bool forceRefresh = false)
        {
            if (Session["CurrentStudentId"] == null)
                Session["CurrentStudentId"] = 0;

            int studentId = (int)Session["CurrentStudentId"];
            List<Registration> registrations = DB.Students.Get(studentId).Registrations;

            if (DB.Registrations.IsMarkedChanged || forceRefresh || Session["StudentsInscriptionsYearsList"] == null)
            {
                List<int> years = new List<int>();
                foreach (Registration registration in registrations.ToList())
                {
                    if (!years.Contains(registration.Year))
                        years.Add(registration.Year);
                }
                Session["StudentsInscriptionsYearsList"] = years;
                return PartialView(DB.Students.Get(studentId).Registrations);
            }
            return Content("");

        }
        public ActionResult IsEmailAvailable(string email, int id = 0)
        {
            bool available = !DB.Students.ToList().Any(s => s.Email == email && s.Id != id);
            return Content(available.ToString().ToLower());
        }

        [UserAccess(Access.Write)]
        public ActionResult Delete()
        {
            if (Session["CurrentStudentId"]!=null)
            {
                int id = (int)Session["CurrentStudentId"];
                DB.Students.Get(id).DeleteAllRegistrations();
                DB.Students.Delete(id);

            }
            
            return RedirectToAction("Index");
        }

        [UserAccess(Access.Admin)]
        public ActionResult Edit()
        {
            int id = (int)Session["CurrentStudentId"];
            Student student = DB.Students.Get(id);
            if (student != null)
            {
                ViewBag.Registrations = student.NextSessionCoursesToSelectList;
                ViewBag.Courses = DB.Courses.NextSessionToSelectList();
                return View(DB.Students.Get(id));
            }
            return RedirectToAction("Index");
        }
        [HttpPost]
        [UserAccess(Access.Admin)]
        public ActionResult Edit(Student student, List<int> selectedCoursesId)
        {
            student.Id = (int)Session["CurrentStudentId"];
            student.Code = DB.Students.Get(student.Id).Code;
            if (student.IsValid() )
            {
                DB.Students.Update(student, selectedCoursesId);
                return RedirectToAction("Details", new { id = student.Id });
            }
            return Redirect("/Accounts/Login?message=Accès illégal! &success=false");
        }

        [UserAccess(Access.Write)]
        public ActionResult Create()
        {
            return View();

        }

        [HttpPost]
        [UserAccess(Access.Write)]
        public ActionResult Create(Student student)
        {
            if (student.IsValid())
            {
                DB.Students.Add(student);
                return RedirectToAction("Index");
            }
            return Redirect("/Accounts/Login?message=Accès illégal! &success=false");
        }



    }
}