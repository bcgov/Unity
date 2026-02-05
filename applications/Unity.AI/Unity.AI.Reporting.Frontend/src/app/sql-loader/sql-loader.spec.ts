import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SqlLoaderComponent } from './sql-loader';

describe('SqlLoader', () => {
  let component: SqlLoaderComponent;
  let fixture: ComponentFixture<SqlLoaderComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SqlLoaderComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SqlLoaderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
