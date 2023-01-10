using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StaffData.Data;
using StaffData.Models;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace StaffData.Controllers
{
    public class EmployeesController : Controller
    {
        private readonly AppDbContext _context;

        public EmployeesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Employees
        // function to get list of employees from database
        public async Task<IActionResult> Index(string sortOrder, string searchString)
        {
            // initialize sortable parameters if exist
            ViewData["IdSortParm"] = String.IsNullOrEmpty(sortOrder) ? "id_desc" : "";
            ViewData["PayrollNumberSortParm"] = String.IsNullOrEmpty(sortOrder) ? "payroll_number_desc" : "";
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["SurnameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "surname_desc" : "";
            ViewData["BirthDateSortParm"] = sortOrder == "Date" ? "birth_date_desc" : "Date";
            ViewData["PhoneSortParm"] = String.IsNullOrEmpty(sortOrder) ? "phone_desc" : "";
            ViewData["MobileSortParm"] = String.IsNullOrEmpty(sortOrder) ? "mobile_desc" : "";
            ViewData["AddressSortParm"] = String.IsNullOrEmpty(sortOrder) ? "address_desc" : "";
            ViewData["CitySortParm"] = String.IsNullOrEmpty(sortOrder) ? "city_desc" : "";
            ViewData["PostcodeSortParm"] = String.IsNullOrEmpty(sortOrder) ? "postcode_desc" : "";
            ViewData["EmailSortParm"] = String.IsNullOrEmpty(sortOrder) ? "email_desc" : "";
            ViewData["RegisterDateSortParm"] = sortOrder == "RDate" ? "register_date_desc" : "Date";
            ViewData["CurrentFilter"] = searchString;

            var employees = from s in _context.Employees
                            select s;

            // if search string exists, code inside if works
            if (!String.IsNullOrEmpty(searchString))
            {
                employees = employees.Where(s => s.Id.Equals(searchString)
                                       || s.PayrollNumber.Contains(searchString)
                                       || s.Name.Contains(searchString)
                                       || s.Surname.Contains(searchString)
                                       || s.Phone.Contains(searchString)
                                       || s.Mobile.Contains(searchString)
                                       || s.Address.Contains(searchString)
                                       || s.City.Contains(searchString)
                                       || s.Postcode.Contains(searchString)
                                       || s.Email.Contains(searchString));
            }

            // switch case for sort by parameters
            switch (sortOrder)
            {
                case "id_desc":
                    employees = employees.OrderByDescending(s => s.Id);
                    break;
                case "name_desc":
                    employees = employees.OrderByDescending(s => s.Name);
                    break;
                case "surname_desc":
                    employees = employees.OrderByDescending(s => s.Surname);
                    break;
                case "birth_date_desc":
                    employees = employees.OrderByDescending(s => s.BirthDate);
                    break;
                case "Date":
                    employees = employees.OrderBy(s => s.BirthDate);
                    break;
                case "phone_desc":
                    employees = employees.OrderByDescending(s => s.Phone);
                    break;
                case "mobile_desc":
                    employees = employees.OrderByDescending(s => s.Mobile);
                    break;
                case "address_desc":
                    employees = employees.OrderByDescending(s => s.Address);
                    break;
                case "city_desc":
                    employees = employees.OrderByDescending(s => s.City);
                    break;
                case "postcode_desc":
                    employees = employees.OrderByDescending(s => s.Postcode);
                    break;
                case "email_desc":
                    employees = employees.OrderByDescending(s => s.Email);
                    break;
                case "register_date_desc":
                    employees = employees.OrderByDescending(s => s.RegisterDate);
                    break;
                case "RDate":
                    employees = employees.OrderBy(s => s.RegisterDate);
                    break;
                default:
                    employees = employees.OrderBy(s => s.Surname);
                    break;
            }

            return View(await employees.AsNoTracking().ToListAsync());
        }

        // function to upload csv files 
        [HttpPost()]
        public async Task<IActionResult> Upload(IFormFile csvFile, [FromServices] IHostingEnvironment hostingEnvironment)
        {
            // file name with url on server
            string fileName = $"{hostingEnvironment.WebRootPath}\\files\\{csvFile.FileName}";

            using (var stream = new FileStream(fileName, FileMode.Create))
            {
                await csvFile.CopyToAsync(stream);
            }

            var records = new List<Employees>();

            using (var reader = new StreamReader(fileName))
            {
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    try
                    {
                        records = csv.GetRecords<Employees>().ToList();
                    }
                    catch (Exception err)
                    {
                        TempData["error"] = "An error occured. " + err;
                        return RedirectToAction(nameof(Index));
                    }
                    
                }
            }

            int size = 0;
            foreach (var record in records)
            {
                // if all fields have data, it will take the row
                if (!String.IsNullOrEmpty(record.PayrollNumber) &&
                    !String.IsNullOrEmpty(record.Firstname) &&
                    !String.IsNullOrEmpty(record.Lastname) &&
                    !String.IsNullOrEmpty(record.DateOfBirth) &&
                    !String.IsNullOrEmpty(record.Phone) &&
                    !String.IsNullOrEmpty(record.Mobile) &&
                    !String.IsNullOrEmpty(record.Address) &&
                    !String.IsNullOrEmpty(record.Address2) &&
                    !String.IsNullOrEmpty(record.Postcode) &&
                    !String.IsNullOrEmpty(record.EMail) &&
                    !String.IsNullOrEmpty(record.StartDate)
                    )
                {
                    var newRecord = new StaffData.Models.Employee();

                    newRecord.PayrollNumber = record.PayrollNumber;
                    newRecord.Name = record.Firstname;
                    newRecord.Surname = record.Lastname;
                    newRecord.BirthDate = Convert.ToDateTime(record.DateOfBirth);
                    newRecord.Phone = record.Phone;
                    newRecord.Mobile = record.Mobile;
                    newRecord.Address = record.Address;
                    newRecord.City = record.Address2;
                    newRecord.Postcode = record.Postcode;
                    newRecord.Email = record.EMail;
                    newRecord.RegisterDate = Convert.ToDateTime(record.StartDate);

                    _context.Add(newRecord);
                    await _context.SaveChangesAsync();
                    size++;
                }

            }

            if (size > 0)
            {
                TempData["success"] = size + " rows has been successfully processed!";
            }
            else
            {
                TempData["error"] = "No any data processed.";
            }

            return RedirectToAction(nameof(Index));

        }

        // GET: Employees/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Employees == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }
            return View(employee);
        }

        // POST: Employees/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,PayrollNumber,Name,Surname,BirthDate,Phone,Mobile,Address,City,Postcode,Email,RegisterDate")] Employee employee)
        {
            if (id != employee.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(employee);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employee.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(employee);
        }

        // GET: Employees/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Employees == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(m => m.Id == id);
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // POST: Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Employees == null)
            {
                return Problem("Entity set 'AppDbContext.Employees'  is null.");
            }
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                _context.Employees.Remove(employee);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EmployeeExists(int id)
        {
          return _context.Employees.Any(e => e.Id == id);
        }

        // class for read csv data for import by columns
        private class Employees
        {
            [Name("Personnel_Records.Payroll_Number")]
            public string PayrollNumber { get; set; }

            [Name("Personnel_Records.Forenames")]
            public string Firstname { get; set; }

            [Name("Personnel_Records.Surname")]
            public string Lastname { get; set; }

            [Name("Personnel_Records.Date_of_Birth")]
            public string DateOfBirth { get; set; }

            [Name("Personnel_Records.Telephone")]
            public string Phone { get; set; }

            [Name("Personnel_Records.Mobile")]
            public string Mobile { get; set; }

            [Name("Personnel_Records.Address")]
            public string Address { get; set; }

            [Name("Personnel_Records.Address_2")]
            public string Address2 { get; set; }

            [Name("Personnel_Records.Postcode")]
            public string Postcode { get; set; }

            [Name("Personnel_Records.EMail_Home")]
            public string EMail { get; set; }

            [Name("Personnel_Records.Start_Date")]
            public string StartDate { get; set; }
        }
    }
}
