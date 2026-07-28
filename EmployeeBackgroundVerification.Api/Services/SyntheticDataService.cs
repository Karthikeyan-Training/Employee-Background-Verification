namespace EmployeeBackgroundVerification.Api.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmployeeBackgroundVerification.Api.Models;
using EmployeeBackgroundVerification.Api.Services.Interfaces;

/// <summary>
/// Provides 20 fully synthetic employee records for testing and demonstration.
/// All names, ID numbers, and addresses are fictitious.
/// No real personal information is used.
/// </summary>
public class SyntheticDataService : ISyntheticDataService
{
    // Catalogue is built once and cached for the lifetime of the service.
    private static readonly IReadOnlyList<SyntheticEmployee> Catalogue = Build();

    public IReadOnlyList<SyntheticEmployee> GetAll() => Catalogue;

    public SyntheticEmployee? GetById(int id) =>
        Catalogue.FirstOrDefault(e => e.Id == id);

    // =========================================================================
    // Data
    // =========================================================================

    private static IReadOnlyList<SyntheticEmployee> Build()
    {
        var raw = new[]
        {
            // id  name                    dob           pan            aadhaar          gender  address                                           degree                    university                   company
            ( 1,  "Arjun Mehta",          "1991-03-15", "TSYNM0001A",  "9999 0000 0001", "Male",   "12, Andheri West, Mumbai, Maharashtra 400058",   "B.Tech Computer Science",  "IIT Delhi",                 "Tata Consultancy Services"),
            ( 2,  "Priya Sharma",         "1993-07-22", "TSYMS0002A",  "9999 0000 0002", "Female", "45, Koramangala 5th Block, Bengaluru, Karnataka 560095", "MBA Finance",        "IIM Ahmedabad",             "HDFC Bank Ltd"),
            ( 3,  "Rahul Gupta",          "1990-11-08", "TSYNG0003A",  "9999 0000 0003", "Male",   "88, Banjara Hills Road No.12, Hyderabad, Telangana 500034", "M.Tech Data Science", "NIT Warangal",         "Infosys Limited"),
            ( 4,  "Sneha Patel",          "1995-02-17", "TSYNP0004A",  "9999 0000 0004", "Female", "23, CG Road, Navrangpura, Ahmedabad, Gujarat 380009", "BCA",                 "Gujarat University",         "Wipro Technologies"),
            ( 5,  "Vikram Singh",         "1988-09-30", "TSYNS0005A",  "9999 0000 0005", "Male",   "7, Anna Nagar East, Chennai, Tamil Nadu 600102",   "B.E. Electronics",       "Anna University",           "HCL Technologies"),
            ( 6,  "Kavitha Reddy",        "1994-05-12", "TSYNR0006A",  "9999 0000 0006", "Female", "56, Jubilee Hills Road No.36, Hyderabad, Telangana 500033", "MCA",             "Osmania University",        "Accenture India"),
            ( 7,  "Ankit Joshi",          "1992-08-25", "TSYNJ0007A",  "9999 0000 0007", "Male",   "14, FC Road, Shivajinagar, Pune, Maharashtra 411005", "B.Sc Computer Science", "Savitribai Phule Pune University", "Capgemini India"),
            ( 8,  "Divya Krishnan",       "1996-01-03", "TSYNK0008A",  "9999 0000 0008", "Female", "32, Thiruvanmiyur, Chennai, Tamil Nadu 600041",     "B.Tech Information Technology", "VIT Vellore",         "IBM India Private Limited"),
            ( 9,  "Rohan Malhotra",       "1989-12-19", "TSYNM0009A",  "9999 0000 0009", "Male",   "91, Vasant Vihar, New Delhi, Delhi 110057",         "MBA Marketing",           "XLRI Jamshedpur",           "Deloitte India"),
            (10,  "Pooja Nair",           "1997-04-06", "TSYNN0010A",  "9999 0000 0010", "Female", "18, Panampilly Nagar, Kochi, Kerala 682036",        "B.Tech CSE",              "Cochin University of Science and Technology", "UST Global"),
            (11,  "Siddharth Rao",        "1991-10-14", "TSYNR0011A",  "9999 0000 0011", "Male",   "63, Gachibowli, Hyderabad, Telangana 500032",       "M.Sc Data Science",       "University of Hyderabad",   "Amazon India Development Centre"),
            (12,  "Meera Iyer",           "1994-06-28", "TSYNI0012A",  "9999 0000 0012", "Female", "9, Avinashi Road, Peelamedu, Coimbatore, Tamil Nadu 641004", "B.E. Computer Science", "PSG College of Technology", "Zoho Corporation"),
            (13,  "Aditya Kumar",         "1990-03-21", "TSYNK0013A",  "9999 0000 0013", "Male",   "27, Whitefield Main Road, Bengaluru, Karnataka 560066", "B.Tech ECE",           "NIT Tiruchirappalli",       "Qualcomm India"),
            (14,  "Lakshmi Pillai",       "1993-09-09", "TSYNP0014A",  "9999 0000 0014", "Female", "4, Vazhuthacaud, Thiruvananthapuram, Kerala 695014", "MCA",                   "Amrita Vishwa Vidyapeetham","Mphasis Limited"),
            (15,  "Nikhil Desai",         "1995-11-27", "TSYND0015A",  "9999 0000 0015", "Male",   "38, Marathahalli Bridge Road, Bengaluru, Karnataka 560037", "B.Tech CSE",       "BITS Pilani",               "Flipkart Internet Private Limited"),
            (16,  "Anjali Bose",          "1992-07-05", "TSYNB0016A",  "9999 0000 0016", "Female", "11, Salt Lake City Sector V, Kolkata, West Bengal 700091", "MBA Human Resources", "University of Calcutta",   "Tata Steel Limited"),
            (17,  "Kiran Verma",          "1987-02-13", "TSYNV0017A",  "9999 0000 0017", "Male",   "52, Sivaji Nagar, Pune, Maharashtra 411005",        "B.Tech Mechanical Engineering", "IIT Roorkee",         "Mahindra and Mahindra Limited"),
            (18,  "Sunita Chandra",       "1996-08-31", "TSYNCS0018A", "9999 0000 0018", "Female", "76, Lajpat Nagar Part II, New Delhi, Delhi 110024", "BCA",                    "University of Delhi",        "Cognizant Technology Solutions"),
            (19,  "Rajesh Mishra",        "1989-05-16", "TSYNM0019A",  "9999 0000 0019", "Male",   "15, Sector 62, Noida, Uttar Pradesh 201309",        "M.Tech Computer Science", "IIT (BHU) Varanasi",        "One97 Communications (Paytm)"),
            (20,  "Deepika Kaur",         "1998-12-04", "TSYNK0020A",  "9999 0000 0020", "Female", "33, Sector 17, Chandigarh, Punjab 160017",           "B.Sc Information Technology", "Panjab University",    "Tech Mahindra Limited"),
        };

        return raw.Select(r => Compose(r.Item1, r.Item2, r.Item3, r.Item4, r.Item5,
                                       r.Item6, r.Item7, r.Item8, r.Item9, r.Item10))
                  .ToList()
                  .AsReadOnly();
    }

    private static SyntheticEmployee Compose(
        int id, string name, string dob, string pan, string aadhaar,
        string gender, string address, string degree, string university, string company)
    {
        return new SyntheticEmployee
        {
            Id              = id,
            Name            = name,
            DateOfBirth     = dob,
            PanNumber       = pan,
            AadhaarNumber   = aadhaar,
            Gender          = gender,
            Address         = address,
            Degree          = degree,
            University      = university,
            PreviousCompany = company,
            ResumeText      = BuildResume(name, dob, pan, aadhaar, address, degree, university, company),
            PanCardText     = BuildPanCard(name, dob, pan),
            AadhaarCardText = BuildAadhaarCard(name, dob, aadhaar, gender, address),
        };
    }

    // =========================================================================
    // Document text builders
    // =========================================================================

    private static string BuildResume(
        string name, string dob, string pan, string aadhaar,
        string address, string degree, string university, string company)
    {
        // Derive role and skill-set from degree
        var (role, skills) = DeriveRoleAndSkills(degree);

        var sb = new StringBuilder();
        sb.AppendLine("CURRICULUM VITAE");
        sb.AppendLine(new string('=', 50));
        sb.AppendLine();
        sb.AppendLine($"Name            : {name}");
        sb.AppendLine($"Date of Birth   : {dob}");
        sb.AppendLine($"PAN             : {pan}");
        sb.AppendLine($"Aadhaar         : {aadhaar}");
        sb.AppendLine($"Address         : {address}");
        sb.AppendLine();
        sb.AppendLine("PROFESSIONAL SUMMARY");
        sb.AppendLine(new string('-', 30));
        sb.AppendLine($"Experienced {role} with a strong academic foundation from {university}. " +
                      $"Proven track record at {company} delivering high-quality solutions " +
                      $"in a fast-paced enterprise environment.");
        sb.AppendLine();
        sb.AppendLine("WORK EXPERIENCE");
        sb.AppendLine(new string('-', 30));
        sb.AppendLine($"Organisation  : {company}");
        sb.AppendLine($"Designation   : {role}");
        sb.AppendLine($"Duration      : {EmploymentYears(dob)} years");
        sb.AppendLine();
        sb.AppendLine("EDUCATION");
        sb.AppendLine(new string('-', 30));
        sb.AppendLine($"Degree        : {degree}");
        sb.AppendLine($"University    : {university}");
        sb.AppendLine($"Year          : {GraduationYear(dob)}");
        sb.AppendLine();
        sb.AppendLine("SKILLS");
        sb.AppendLine(new string('-', 30));
        sb.AppendLine(skills);
        sb.AppendLine();
        sb.AppendLine("DECLARATION");
        sb.AppendLine(new string('-', 30));
        sb.AppendLine("I hereby declare that all the information provided above is true and correct " +
                      "to the best of my knowledge and belief.");
        sb.AppendLine($"Place: {CityFromAddress(address)}");
        sb.AppendLine($"Date : {dob[..7]}-28");  // same year-month, day 28
        return sb.ToString();
    }

    private static string BuildPanCard(string name, string dob, string pan)
    {
        var sb = new StringBuilder();
        sb.AppendLine("INCOME TAX DEPARTMENT");
        sb.AppendLine("GOVT. OF INDIA");
        sb.AppendLine();
        sb.AppendLine("PERMANENT ACCOUNT NUMBER CARD");
        sb.AppendLine();
        sb.AppendLine($"Name          : {name.ToUpperInvariant()}");
        sb.AppendLine($"Father's Name : {SyntheticFathersName(name)}");
        sb.AppendLine($"Date of Birth : {FormatDobForCard(dob)}");
        sb.AppendLine();
        sb.AppendLine($"PAN           : {pan}");
        sb.AppendLine();
        sb.AppendLine("This card is issued under section 139A of the Income Tax Act, 1961.");
        sb.AppendLine("[SYNTHETIC TEST RECORD — NOT A REAL PAN CARD]");
        return sb.ToString();
    }

    private static string BuildAadhaarCard(
        string name, string dob, string aadhaar, string gender, string address)
    {
        var sb = new StringBuilder();
        sb.AppendLine("भारत सरकार / GOVERNMENT OF INDIA");
        sb.AppendLine("Unique Identification Authority of India");
        sb.AppendLine("आधार / AADHAAR");
        sb.AppendLine();
        sb.AppendLine($"Name          : {name}");
        sb.AppendLine($"Date of Birth : {FormatDobForCard(dob)}");
        sb.AppendLine($"Gender        : {gender}");
        sb.AppendLine();
        sb.AppendLine($"Address:");
        sb.AppendLine($"  {address}");
        sb.AppendLine();
        sb.AppendLine($"Aadhaar No.   : {aadhaar}");
        sb.AppendLine();
        sb.AppendLine("This Aadhaar is issued by UIDAI.");
        sb.AppendLine("[SYNTHETIC TEST RECORD — NOT A REAL AADHAAR CARD]");
        return sb.ToString();
    }

    // =========================================================================
    // Helper utilities
    // =========================================================================

    private static (string Role, string Skills) DeriveRoleAndSkills(string degree)
    {
        var d = degree.ToLowerInvariant();

        if (d.Contains("computer science") || d.Contains("cse") || d.Contains("software"))
            return ("Software Engineer",
                "C#, Java, Python, SQL, REST APIs, Microservices, Git, Agile");

        if (d.Contains("data science") || d.Contains("data"))
            return ("Data Scientist",
                "Python, R, TensorFlow, PyTorch, SQL, Tableau, Machine Learning, Statistics");

        if (d.Contains("information technology") || d.Contains("it"))
            return ("IT Analyst",
                "Networking, ITIL, Cloud (AWS/Azure), Linux, SQL, Python, JIRA");

        if (d.Contains("electronics") || d.Contains("ece"))
            return ("Embedded Systems Engineer",
                "C, C++, VLSI, PCB Design, MATLAB, Embedded Linux, Python");

        if (d.Contains("mechanical"))
            return ("Mechanical Design Engineer",
                "AutoCAD, SolidWorks, CATIA, ANSYS, GD&T, Lean Manufacturing");

        if (d.Contains("mba") && d.Contains("finance"))
            return ("Financial Analyst",
                "Financial Modelling, Excel, Bloomberg, SAP FICO, Power BI, CFA Level II");

        if (d.Contains("mba") && d.Contains("marketing"))
            return ("Marketing Manager",
                "Google Analytics, SEO/SEM, Salesforce, Adobe Creative Suite, SQL");

        if (d.Contains("mba") && d.Contains("human"))
            return ("HR Business Partner",
                "SAP SuccessFactors, Workday, Recruitment, Payroll, Labour Law, Excel");

        if (d.Contains("mca") || d.Contains("bca"))
            return ("Application Developer",
                "Java, Spring Boot, React, SQL, Docker, REST APIs, Git");

        if (d.Contains("b.sc") || d.Contains("m.sc"))
            return ("Systems Analyst",
                "Python, SQL, Excel, Tableau, Linux, API Integration");

        return ("Technology Professional",
                "Problem Solving, Communication, Project Management, Microsoft Office, SQL");
    }

    private static int EmploymentYears(string dob)
    {
        if (DateTime.TryParse(dob, out var birth))
        {
            int age = DateTime.Today.Year - birth.Year;
            return Math.Max(1, age - 22); // assume joined ~22
        }
        return 3;
    }

    private static int GraduationYear(string dob)
    {
        if (DateTime.TryParse(dob, out var birth))
            return birth.Year + 22;
        return 2015;
    }

    private static string FormatDobForCard(string dob)
    {
        if (DateTime.TryParse(dob, out var d))
            return d.ToString("dd/MM/yyyy");
        return dob;
    }

    private static string CityFromAddress(string address)
    {
        // Return the city portion (generally the second-to-last comma segment)
        var parts = address.Split(',');
        return parts.Length >= 2 ? parts[^2].Trim() : address;
    }

    private static string SyntheticFathersName(string name)
    {
        // Generate a plausible but clearly synthetic father's name
        var parts = name.Split(' ');
        string lastName = parts.Length > 1 ? parts[^1] : parts[0];
        string[] prefixes = { "Shri", "Sri", "Late" };
        string[] fatherFirstNames = { "Ramesh", "Suresh", "Mahesh", "Dinesh", "Naresh",
                                      "Ganesh", "Rakesh", "Rajesh", "Mukesh", "Umesh" };
        // Use hash of name for deterministic selection
        int h = Math.Abs(name.GetHashCode());
        return $"{prefixes[h % prefixes.Length]} {fatherFirstNames[h % fatherFirstNames.Length]} {lastName}";
    }
}
