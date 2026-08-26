import io
import os
import sys

from pypdf import PdfReader, PdfWriter, Transformation
from reportlab.lib import colors
from reportlab.lib.pagesizes import letter
from reportlab.lib.units import inch
from reportlab.pdfgen import canvas


SLATE = colors.HexColor("#475569")
LIGHT_SLATE = colors.HexColor("#94A3B8")
PROJECT = "ELCOP - Gestao de TI"


def decoration(page_number):
    stream = io.BytesIO()
    pdf = canvas.Canvas(stream, pagesize=letter)
    pdf.setStrokeColor(LIGHT_SLATE)
    pdf.setLineWidth(0.5)
    pdf.line(0.75 * inch, letter[1] - 0.65 * inch, letter[0] - 0.75 * inch, letter[1] - 0.65 * inch)
    pdf.setFont("Helvetica", 8.5)
    pdf.setFillColor(SLATE)
    pdf.drawString(0.75 * inch, letter[1] - 0.55 * inch, "Auditoria Tecnica de Software")
    pdf.drawRightString(letter[0] - 0.75 * inch, letter[1] - 0.55 * inch, PROJECT)
    pdf.line(0.75 * inch, 0.6 * inch, letter[0] - 0.75 * inch, 0.6 * inch)
    pdf.drawString(0.75 * inch, 0.42 * inch, "Gerado automaticamente - validar achados criticos antes de agir")
    pdf.drawRightString(letter[0] - 0.75 * inch, 0.42 * inch, f"Pagina {page_number}")
    pdf.save()
    stream.seek(0)
    return PdfReader(stream).pages[0]


def main():
    if len(sys.argv) != 2:
        raise SystemExit("Uso: normalize-even-pages.py relatorio.pdf")

    source = sys.argv[1]
    temporary = source + ".tmp"
    reader = PdfReader(source)
    writer = PdfWriter()

    for page_number, page in enumerate(reader.pages, start=1):
        if page_number >= 4 and page_number % 2 == 0:
            page.add_transformation(Transformation().translate(tx=0, ty=-45))
            page.merge_page(decoration(page_number))
        writer.add_page(page)

    with open(temporary, "wb") as stream:
        writer.write(stream)
    os.replace(temporary, source)


if __name__ == "__main__":
    main()
