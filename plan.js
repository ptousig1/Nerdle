
console.log("plan.js loaded");

document.getElementById("loadingSpan").style.display = "none";

let mainSpan = document.getElementById("mainSpan");
mainSpan.style.display = "";

let guessTable = document.getElementById("guessTable");
let tableBody = document.getElementById("tableBody");

function AddGuess(a_rowNum, a_plan)
	{
	if (a_plan == null)
		{
		let tr = tableBody.appendChild(document.createElement("TR"));
		let td = tr.appendChild(document.createElement("TD"));
		td.colSpan = 9;
		td.innerText = "Inconceivable!";
		document.getElementById("answerListCount").innerText = "no";
		}
	else
		{
		let guess = a_plan.Guess;

		let tr = tableBody.appendChild(document.createElement("TR"));
		tr.rowNum = a_rowNum;
		tr.plan = a_plan;
		for(let i=0; i<8; i++)
			{
			let td = tr.appendChild(document.createElement("TD"));
			td.id = "row" + a_rowNum + "col" + i;
			td.index = i;
			td.style.width = "14px";
			td.style.textAlign = "center";
			let c = guess.substr(i, 1);
			if (c == '/')
				c = "&divide;";
			if (c == '*')
				c = "&times;";
			td.innerHTML = c;
			td.style.color = "#FFFFFF";
			td.style.cursor = "pointer";
			td.onclick = SymbolClick;
			}

		if (Object.keys(a_plan.Hints).length == 1)
			tr.hint = "GGGGGGGG";
		else
			tr.hint = "........";

		ColourizeGuess(tr);

		let td = tr.appendChild(document.createElement("TD"));
		let button = td.appendChild(document.createElement("BUTTON"));
		button.id = "row" + a_rowNum + "button";
		button.type = "button";
		button.innerText = "Set";
		button.onclick = SetButtonClick;

		document.getElementById("answerListCount").innerText = a_plan.PossibleAnswerCount;
		}
	}

function ColourizeGuess(tr)
	{
	let rowNum = tr.rowNum;
	let hint = tr.hint;
	for(let i=0; i<8; i++)
		{
		let td = document.getElementById("row" + rowNum + "col" + i);
		let c = hint.substr(i, 1);
		if (c == '.')
			td.style.backgroundColor = "#000000";
		if (c == 'p')
			td.style.backgroundColor = "#E12AFB";
		if (c == 'G')
			td.style.backgroundColor = "#7CCF35";
		}
	}

function SymbolClick(e)
	{
	let td = e.currentTarget;
	let tr = td.parentNode;
	i = td.index;
	let hint = tr.hint;
	let c = hint.substr(i,1);
	if (c == '.')
		c = 'p';
	else if (c == 'p')
		c = 'G';
	else if (c == 'G')
		c = '.';
	let newHint = "";
	if (i > 0)
		newHint += hint.substr(0, i);
	newHint += c;
	if (i < 7)
		newHint += hint.substr(i+1);
	tr.hint = newHint;
	ColourizeGuess(tr);
	}

function SetButtonClick(e)
	{
	let button = e.currentTarget;
	let td = button.parentNode;
	let tr = td.parentNode;
	let rowNum = tr.rowNum;

	button.style.display = "none";
	for(let i=0; i<8; i++)
		{
		let td = document.getElementById("row" + rowNum + "col" + i);
		td.onclick = null;
		}

	let plan = tr.plan;
	let hint = tr.hint;
	plan = plan.Hints[hint];

	AddGuess(rowNum + 1, plan);
	}

AddGuess(0, g_plan);

/*
	let displayGuess = function()
		{
		for(let i=0; i<8; i++)
			{
			let td = document.getElementById("pos" + i);
			td.index = i;
			td.style.cursor = "pointer";
			let c = guess.substr(i, 1);
			if (c == '/')
				c = "&divide;";
			if (c == '*')
				c = "&times;";
			td.innerText = c;
			td.style.color = "#FFFFFF";
			c = hint.substr(i, 1);
			if (c == '.')
				td.style.backgroundColor = "#000000";
			if (c == 'p')
				td.style.backgroundColor = "#E12AFB";
			if (c == 'G')
				td.style.backgroundColor = "#7CCF35";
		
			td.onclick = function(e)
				{
				let td = e.currentTarget;
				i = td.index;
				let c = hint.substr(i,1);
				if (c == '.')
					c = 'p';
				else if (c == 'p')
					c = 'G';
				else if (c == 'G')
					c = '.';
				let newHint = "";
				if (i > 0)
					newHint += hint.substr(0, i);
				newHint += c;
				if (i < 7)
					newHint += hint.substr(i+1);
				hint = newHint;
				displayGuess();
				};
			}

		let button = document.getElementById("setButton");
		button.onclick = function(e)
			{
			plan = a_plan.Hints[hint];
			AddGuess(plan);
			button.style.display = "none";
			};
		}

	displayGuess();

	document.getElementById("answerListCount").innerText = a_plan.PossibleAnswerCount;
	}
*/


