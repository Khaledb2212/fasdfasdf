    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Web_API.Models;
    using Microsoft.AspNetCore.Authorization;
    using System.Security.Claims;
    using Web_API.DTOs;



    namespace Web_API.Controllers
    {
        //[Route("api/[controller]")]
        [Authorize]
        [Route("api/People")]
        [ApiController]
        public class PeopleController : Controller
        {
            private readonly ProjectDbContext _context;

            // 1. Dependency Injection: We inject the DB context instead of creating "new"
            public PeopleController(ProjectDbContext context)
            {
                _context = context;
            }

            //https://localhost:7085/api/People/GetPeople
            [HttpGet("GetPeople", Name = "GetPeople")]
            [ProducesResponseType(StatusCodes.Status200OK)]
            [ProducesResponseType(StatusCodes.Status500InternalServerError)]
            public async Task<ActionResult<IEnumerable<Person>>> GetPeople()
            {
                try
                {
                    var peopleList = await _context.People.ToListAsync();

                    // Optional: If you want to return 404 if the table is empty
                    if (peopleList == null || peopleList.Count == 0)
                    {
                        return NotFound("No records found in the database.");
                    }

                    return Ok(peopleList); // Status 200
                }
                catch (Exception ex)
                {
                    // Log the error (ex) here in a real app
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        "Error retrieving data from the database");
                }
            }

            //https://localhost:7085/api/People/GetPerson?id=
            [HttpGet("GetPerson", Name = "GetPerson")]
            [ProducesResponseType(StatusCodes.Status200OK)]
            [ProducesResponseType(StatusCodes.Status404NotFound)]
            [ProducesResponseType(StatusCodes.Status500InternalServerError)]
            public async Task<ActionResult<Person>> GetPerson(int id)
            {
                try
                {
                    var person = await _context.People.FindAsync(id);

                    if (person == null)
                    {
                        return NotFound($"Person with Id = {id} not found"); // Status 404
                    }

                    return Ok(person); // Status 200
                }
                catch (Exception ex)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        "Error retrieving data from the database");
                }
            }


            //https://localhost:7085/api/People/PutPerson?id=
            [HttpPut("PutPerson", Name = "PutPerson")]
        
            [ProducesResponseType(StatusCodes.Status204NoContent)]
            [ProducesResponseType(StatusCodes.Status400BadRequest)]
            [ProducesResponseType(StatusCodes.Status404NotFound)]
            [ProducesResponseType(StatusCodes.Status500InternalServerError)]
            public async Task<IActionResult> PutPerson(int id, Person person)
            {
                if(id < 0 )
                {
                    return BadRequest("This Id Is Invalid");
                }


                if (id != person.PersonID)
                {
                    return BadRequest("Person ID mismatch"); // Status 400
                }

                _context.Entry(person).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PersonExists(id))
                    {
                        return NotFound($"Person with Id = {id} not found"); // Status 404
                    }
                    else
                    {
                        // If it's a concurrency error but the person exists, rethrow strictly
                        return StatusCode(StatusCodes.Status500InternalServerError, "Concurrency error occurred");
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        "Error updating data");
                }

                return NoContent(); // Status 204 (Success, but nothing to return)
            }



            // Called from MVC right after Identity user is created.
            //https://localhost:7085/api/People/PostPerson
            [HttpPost("PostPerson", Name = "PostPerson")]
            [AllowAnonymous]
            [ProducesResponseType(StatusCodes.Status201Created)]
            [ProducesResponseType(StatusCodes.Status400BadRequest)]
            [ProducesResponseType(StatusCodes.Status500InternalServerError)]
            public async Task<ActionResult<Person>> PostPerson(CreatePersonDto dto)
            {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized("No authenticated user (cookie not received/decrypted).");

            if (!ModelState.IsValid) return BadRequest(ModelState);

                bool exists = await _context.People.AnyAsync(p => p.UserId == userId);
                if (exists) return Conflict("Person already exists for this user.");

                var person = new Person
                {
                    UserId = userId,
                    Firstname = dto.Firstname,
                    Lastname = dto.Lastname,
                    Phone = dto.Phone
                };

                _context.People.Add(person);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetPerson), new { id = person.PersonID }, person);

            }



            // Requires cookie auth (shared cookie) and returns the current user's Person.
            [HttpGet("GetAuthenticatedPerson")]
            [Authorize]
            public async Task<ActionResult<Person>> GetAuthenticatedPerson()
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

                var person = await _context.People.FirstOrDefaultAsync(p => p.UserId == userId);
                if (person == null) return NotFound("No Person profile found for this user.");

                return Ok(person);
            }


            //https://localhost:7085/api/People/DeletePerson?id=
            [HttpDelete("DeletePerson", Name = "DeletePerson")]
            [ProducesResponseType(StatusCodes.Status204NoContent)]
            [ProducesResponseType(StatusCodes.Status404NotFound)]
            [ProducesResponseType(StatusCodes.Status500InternalServerError)]
            public async Task<IActionResult> DeletePerson(int id)
            {
                try
                {
                    var person = await _context.People.FindAsync(id);
                    if (person == null)
                    {
                        return NotFound($"Person with Id = {id} not found");
                    }

                    _context.People.Remove(person);
                    await _context.SaveChangesAsync();

                    return NoContent(); // Status 204
                }
                catch (Exception ex)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError,
                        "Error deleting data");
                }
            }

            [HttpGet("GetByUserId")]
            [AllowAnonymous]
            public async Task<ActionResult<Person>> GetByUserId(string userId)
            {
                if (string.IsNullOrWhiteSpace(userId)) return BadRequest("userId is required.");

                var person = await _context.People.FirstOrDefaultAsync(p => p.UserId == userId);
                if (person == null) return NotFound();

                return Ok(person);
            }

            private bool PersonExists(int id)
            {
                return _context.People.Any(e => e.PersonID == id);
            }
        }
    }
