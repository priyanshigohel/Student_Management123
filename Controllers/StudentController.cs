using ExcelDataReader;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.StaticFiles;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Student_Management.DAL;
using Student_Management.DAL.Repository;
using Student_Management.Model.Models;
using Student_Management.Models;
using System.Diagnostics.Metrics;
using System.Reflection.Metadata.Ecma335;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Build.Tasks;

namespace Student_Management.Controllers
{
	public class StudentController : Controller
	{
		private readonly ApplicationDbContext _db;
		private readonly IWebHostEnvironment _environment;
		//private readonly IStudentRepository _studentRepository;

		public StudentController(ApplicationDbContext db, IWebHostEnvironment environment)
		{
			_db = db;
			_environment = environment;
		
			//_studentRepository = studentRepository;
		}

		public IActionResult Index(string? name)
		{
			
			if (name == null)
			{
				IEnumerable<Student> students = _db.students.ToList();
				return View(students);
			}
			else
			{
				ViewBag.SearchStr = name;
				IEnumerable<Student> students = _db.students.Where(u => u.Name.ToLower().Contains(name)).ToList();
				return View(students);
			}
		}
		[HttpGet]
		public IActionResult Login()
		{
			return View();
		}
		[HttpPost]
		public IActionResult Login(Admindb admin)
		{
			var user = _db.admin.FirstOrDefault(u => u.AdminEmail == admin.AdminEmail && u.AdminPassword == admin.AdminPassword);
			if (user != null) 
			{
				TempData["success"] = "Login successfully";
				return RedirectToAction("Index");
			}
			else 
			{
				TempData["error"] = "Invalid Username or password";
				return RedirectToAction("Login");
			}
			
		}

		[HttpGet]
		public IActionResult AddStudent()
		{
			return View();
		}

		[HttpPost]
		public IActionResult AddStudent(Student student)
		{
			if(ModelState.IsValid) 
			{
				//_studentRepository.AddStudent(student);
				_db.students.Add(student);
				_db.SaveChanges();
				TempData["success"] = "Student added successfully";
				return RedirectToAction("Index");
			}
			return View(student);
			
		}

		[HttpGet]
		public IActionResult EditStudent(int id) 
		{
			//Student student = _studentRepository.GetStudentByID(id);
			Student student = _db.students.Find(id);
			return View(student);
		}
		[HttpPost]
		public IActionResult EditStudent(Student student)
		{
			if(ModelState.IsValid)
			{
				//_studentRepository.UpdateStudent(student);
				_db.students.Update(student);
				_db.SaveChanges();
				TempData["success"] = "Student Updated successfully";
				return RedirectToAction("Index");
			}
			return View(student);
		}
		
	
		public IActionResult DeleteStudent(int id)
		{
			//_studentRepository.DeleteStudent(id);
			var data = _db.students.Find(id);

			_db.studcourse.Where(u => u.StudentId == id).ToList().ForEach(u => _db.studcourse.Remove(u));
			_db.SaveChanges();

			_db.students.Remove(data);
			_db.SaveChanges();
			TempData["success"] = "Student Deleted successfully";
			return RedirectToAction("Index");
		}


		[HttpGet]
		public IActionResult ViewCourse(int? id)
		{
			IEnumerable<StudCourse> studcourse = _db.studcourse.Where(u => u.StudentId == id).ToList();
			return View(studcourse);
		}

		[HttpGet]
		public IActionResult AddCourse(int? id)
		{
			Student student = _db.students.Find(id);
			ViewBag.Student = student.StudentId;

			VMCourse cource = new()
			{
				courseList = _db.courses.Select(u => new SelectListItem
				{
					Text = u.CourseName,
					Value = u.CourseId.ToString()
				}),
				course = new Course()
			};
			return View(cource);
		}


		[HttpPost]
		public IActionResult AddCourse(VMCourse model, int id)
		{
			var coursesNM = _db.courses.Where(X=> X.CourseId == model.course.CourseId).FirstOrDefault();
			var student = _db.students.FirstOrDefault(u => u.StudentId == id);
			var studcourse = _db.studcourse.Where(u => u.CourseId == model.course.CourseId && u.StudentId == id).FirstOrDefault();

			if(studcourse == null)
			{ 
				StudCourse course = new StudCourse()
				{
					CourseId = model.course.CourseId,
					StudentId = id,
					CourseName = coursesNM.CourseName
				};

				
				_db.Add(course);
				_db.SaveChanges();
				TempData["success"] = "Course Added successfully";
			}
			else
			{
				TempData["error"] = "This Course is already asign";
				return RedirectToAction("Index");
			}
			
			

		   var courses =  _db.courses.Where(x => x.CourseId == model.course.CourseId).FirstOrDefault();
			var courseprice = courses.CoursePrice;


			if(student.CourseTotalPrice == null)
			{
				student.CourseTotalPrice =  courseprice;
			}
			else
			{
				student.CourseTotalPrice += courseprice;
			}

			//student.CourseTotalPrice = student.CourseTotalPrice += courses.CoursePrice;

			Student st = new Student()
			{
				CourseTotalPrice = student.CourseTotalPrice,
			};
			_db.SaveChanges();
		
			return RedirectToAction("Index");
		
		}

		public IActionResult DeleteViewCourse(int? id)
		{
			var stud = _db.studcourse.Where(c => c.CourseId == id).First();
			//var Student = _db.students.Where(x => x.StudentId == stud.StudentId).FirstOrDefault();		
			var result = _db.studcourse.Where(u => u.StudentId == stud.StudentId && u.CourseId == stud.CourseId).ToList();

			_db.studcourse.RemoveRange(result);
			_db.SaveChanges();

			var student = _db.students.Where(u => u.StudentId == stud.StudentId).FirstOrDefault();
			var courses = _db.courses.Where(x => x.CourseId == stud.CourseId).FirstOrDefault();
			var courseprice = courses.CoursePrice;

			if (student.CourseTotalPrice == null)
			{
				student.CourseTotalPrice = courseprice;
			}
			else
			{
				student.CourseTotalPrice -= courseprice;
			}
			_db.SaveChanges();

			TempData["success"] = "Course Deleted successfully";

			return RedirectToAction("Index");
		}

		[HttpPost]
		public IActionResult UploadData(IFormFile file)
		{
			var students = new List<Student>();
			
			System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
			using (var stream = file.OpenReadStream())
			{
				using (var reader = ExcelReaderFactory.CreateReader(stream))
				{

					while (reader.Read())
					{
							students.Add(new Student
							{
							Name = reader.GetString(0),
							RollNo = int.Parse(reader.GetValue(1).ToString()),
							Email = reader.GetString(2),
							Address = reader.GetString(3),
							State = reader.GetString(4),
							City = reader.GetString(5),
							ZipCode = reader.GetString(6),
							ContactNo = int.Parse(reader.GetValue(7).ToString()),
							CourseTotalPrice = double.Parse(reader.GetValue(8).ToString())
							});
					}
				}
			}

			_db.students.AddRange(students);
			_db.SaveChanges();
			TempData["success"] = "File Imported Successfully";
			return RedirectToAction("Index");

		}
	}
}
