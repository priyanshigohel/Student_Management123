using Microsoft.AspNetCore.Mvc;
using Student_Management.DAL.Repository;
using Student_Management.Model.Models;

namespace Student_Management.Controllers
{
	public class CourseController : Controller
	{
		private readonly ICourseRepository _courseRepository;
		public CourseController(ICourseRepository courseRepository)
		{
			_courseRepository = courseRepository;
		}

		public IActionResult CourseIndex(string? name)
		{
			if (name == null)
			{
				IEnumerable<Course> courses = _courseRepository.GetCourses();
				return View(courses);
			}
			else
			{
				IEnumerable<Course> courses = _courseRepository.GetCourses().Where(u => u.CourseName.ToLower().Contains(name));
				return View(courses);
			}
		}

		[HttpGet]
		public IActionResult AddCourse()
		{
			return View();
		}

		[HttpPost]
		public IActionResult AddCourse(Course course)
		{
			if (ModelState.IsValid)
			{
				var data = _courseRepository.AddCourse(course);
				if(data == true)
				{
					TempData["success"] = "Course added successfully";
					return RedirectToAction("CourseIndex");
				}
				else
				{
					TempData["error"] = "Course already exists";
					return RedirectToAction("CourseIndex");
				}
				
			}
			return View(course);

		}

		[HttpGet]
		public IActionResult EditCourse(int id)
		{
			Course course = _courseRepository.GetCourseByID(id);
			return View(course);
		}
		[HttpPost]
		public IActionResult EditCourse(Course course)
		{
			if (ModelState.IsValid)
			{
				var data =_courseRepository.UpdateCourse(course);
				if (data == true)
				{
					TempData["success"] = "Course Updated successfully";
					return RedirectToAction("CourseIndex");
				}
				else
				{
					TempData["error"] = "Course Can't be edit because course is asign to some students";
					return RedirectToAction("CourseIndex");
				}
			}
			return View(course);
		}

		public IActionResult DeleteCourse(int id)
		{
			var data=_courseRepository.DeleteCourse(id);
			if(data==true)
			{
				TempData["success"] = "Course Deleted successfully";
				return RedirectToAction("CourseIndex");
			}
			else
			{
				TempData["error"] = "Course Can't be delete because course is asign to some students";
				return RedirectToAction("CourseIndex");
			}
								
		}

	}
}
