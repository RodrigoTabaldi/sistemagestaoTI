import importlib.util
import json
import sys

from reportlab.lib.pagesizes import letter
from reportlab.lib.units import inch
from reportlab.platypus import NextPageTemplate, SimpleDocTemplate


BUILDER = r"C:\Users\rodri\.codex\skills\senior-repo-audit\scripts\build_report_pdf.py"


def load_builder():
    spec = importlib.util.spec_from_file_location("audit_pdf_builder", BUILDER)
    if spec is None or spec.loader is None:
        raise RuntimeError("Nao foi possivel carregar o gerador de relatorios.")

    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main():
    if len(sys.argv) != 3:
        raise SystemExit("Uso: build-audit-with-stable-body.py audit.json relatorio.pdf")

    module = load_builder()

    with open(sys.argv[1], "r", encoding="utf-8") as stream:
        data = json.load(stream)

    styles = module.build_styles()
    decorator = module.PageDecorator(data.get("meta", {}).get("project_name", "Projeto"))
    story = []
    story += module.build_cover(data, styles)
    story += module.build_executive_summary(data, styles)
    for section in data.get("sections", []):
        story += module.build_section(section, styles)
    story += module.build_roadmap(data.get("roadmap", []), styles)
    story += module.build_appendix(data, styles)
    story = [flowable for flowable in story if not isinstance(flowable, NextPageTemplate)]

    document = SimpleDocTemplate(
        sys.argv[2],
        pagesize=letter,
        leftMargin=0.75 * inch,
        rightMargin=0.75 * inch,
        topMargin=0.75 * inch,
        bottomMargin=0.75 * inch,
        title=f"Auditoria Tecnica - {data.get('meta', {}).get('project_name', 'Projeto')}",
    )

    def cover_or_body(pdf_canvas, doc):
        if doc.page == 1:
            decorator.cover(pdf_canvas, doc)
        else:
            decorator.body(pdf_canvas, doc)

    document.build(story, onFirstPage=cover_or_body, onLaterPages=decorator.body)


if __name__ == "__main__":
    main()
